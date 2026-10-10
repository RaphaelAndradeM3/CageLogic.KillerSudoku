using System.Globalization;
using CageLogic.Application.Difficulty;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Progression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CageLogic.Infrastructure.Progression;

/// <summary>Transactional SQLite adapter for active sessions and compact statistics records.</summary>
public sealed class SqliteGameProgressStore : IGameProgressStore
{
	private const string RecordColumns =
		"SessionId, Status, StartedAtUtc, CompletedAtUtc, AbandonedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount";
	private const string SessionColumns = RecordColumns + ", SnapshotVersion, SnapshotJson";
	private static readonly GameSessionPersistenceMapper SnapshotMapper = new();
	private readonly SqliteConnectionFactory _connectionFactory;
	private readonly ILogger<SqliteGameProgressStore> _logger;
	private readonly object _schemaMigrationLock = new();
	private bool _schemaMigrated;

	public SqliteGameProgressStore(
		SqliteConnectionFactory connectionFactory,
		ILogger<SqliteGameProgressStore>? logger = null)
	{
		ArgumentNullException.ThrowIfNull(connectionFactory);
		_connectionFactory = connectionFactory;
		_logger = logger ?? NullLogger<SqliteGameProgressStore>.Instance;
	}

	public Task<GameProgressWriteResult> CreateAsync(
		SavedGameSession session,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => Create(session), cancellationToken);

	public Task<GameProgressLoadResult> LoadActiveAsync(CancellationToken cancellationToken = default) =>
		RunInBackground(LoadActive, cancellationToken);

	public Task<GameProgressWriteResult> SaveAsync(
		SavedGameSession session,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => Save(session), cancellationToken);

	public Task<GameProgressWriteResult> CompleteAsync(
		GameProgressRecord completedRecord,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => TransitionToTerminal(completedRecord, GameProgressStatus.Completed), cancellationToken);

	public Task<GameProgressWriteResult> AbandonAsync(
		GameProgressRecord abandonedRecord,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => TransitionToTerminal(abandonedRecord, GameProgressStatus.Abandoned), cancellationToken);

	public Task<GameProgressWriteResult> AbandonUnrecoverableActiveAsync(
		string sessionKey,
		DateTimeOffset abandonedAtUtc,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(sessionKey);
		return RunInBackground(() => AbandonUnrecoverableActive(sessionKey, abandonedAtUtc), cancellationToken);
	}

	public Task<GameProgressWriteResult> ReplaceActiveAsync(
		SavedGameSession replacement,
		string? replacedSessionKey,
		DateTimeOffset abandonedAtUtc,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => ReplaceActive(replacement, replacedSessionKey, abandonedAtUtc), cancellationToken);

	public Task<IReadOnlyList<GameProgressRecord>> GetRecordsAsync(CancellationToken cancellationToken = default) =>
		RunInBackground(GetRecords, cancellationToken);

	private GameProgressWriteResult Create(SavedGameSession session)
	{
		if (!IsValidActiveSession(session, out var reason))
			return GameProgressWriteResult.Failed(reason);

		try
		{
			using var connection = OpenWriter();
			using var transaction = connection.BeginTransaction();
			var existing = FindSession(connection, transaction, session.SessionId);
			if (existing is not null)
			{
				using var update = connection.CreateCommand();
				update.Transaction = transaction;
				update.CommandText = """
					UPDATE GameSessions
					SET StartedAtUtc = $started, Difficulty = $difficulty,
						ActiveElapsedTicks = $elapsed, ErrorCount = $errors,
						DisplayedHintLevelCount = $hints, SnapshotVersion = $snapshotVersion,
						SnapshotJson = $snapshotJson
					WHERE SessionId = $id AND Status = 'Active';
					""";
				AddRecordParameters(update, session.Record);
				update.Parameters.AddWithValue("$snapshotVersion", session.SnapshotVersion);
				update.Parameters.AddWithValue("$snapshotJson", session.SnapshotJson);
				if (update.ExecuteNonQuery() == 1)
				{
					transaction.Commit();
					return GameProgressWriteResult.Saved();
				}
				transaction.Commit();
				return GameProgressWriteResult.Conflict("SessionIdAlreadyExists");
			}

			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText =
				"INSERT INTO GameSessions (" + SessionColumns + ") VALUES ($id, $status, $started, NULL, NULL, $difficulty, $elapsed, $errors, $hints, $snapshotVersion, $snapshotJson);";
			AddRecordParameters(command, session.Record);
			command.Parameters.AddWithValue("$snapshotVersion", session.SnapshotVersion);
			command.Parameters.AddWithValue("$snapshotJson", session.SnapshotJson);
			command.ExecuteNonQuery();
			transaction.Commit();
			return GameProgressWriteResult.Saved();
		}
		catch (SqliteException exception)
		{
			LogWriteFailure("Create", session.SessionId, exception);
			return IsUniqueConstraintViolation(exception)
				? GameProgressWriteResult.Conflict("ActiveSessionAlreadyExists")
				: GameProgressWriteResult.Failed("DatabaseWriteFailed");
		}
	}

	private GameProgressLoadResult LoadActive()
	{
		using var connection = OpenReader();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {SessionColumns} FROM GameSessions WHERE Status = 'Active' LIMIT 1;";
		using var reader = command.ExecuteReader();
		if (!reader.Read())
			return GameProgressLoadResult.NoActiveSession();

		var recoverySessionKey = reader.GetString(0);
		GameProgressRecord record;
		try
		{
			record = ReadRecord(reader);
		}
		catch (Exception exception) when (IsInvalidPersistedRecord(exception))
		{
			_logger.LogWarning(exception,
				"Active game progress row {SessionKey} contains invalid persisted fields.",
				recoverySessionKey);
			return RecoveryRequired(null, "ActiveRecordInvalid", recoverySessionKey);
		}

		if (reader.IsDBNull(9) || reader.IsDBNull(10))
			return RecoveryRequired(record, "ActiveSnapshotMissing", recoverySessionKey);

		int snapshotVersion;
		string snapshotJson;
		try
		{
			snapshotVersion = reader.GetInt32(9);
			snapshotJson = reader.GetString(10);
		}
		catch (Exception exception) when (IsInvalidPersistedRecord(exception))
		{
			return RecoveryRequired(record, "ActiveSnapshotInvalid", recoverySessionKey);
		}
		if (snapshotVersion != GameSessionPersistenceSnapshot.CurrentVersion)
			return RecoveryRequired(record, "SnapshotVersionUnsupported", recoverySessionKey);

		try
		{
			var snapshot = SnapshotMapper.Deserialize(snapshotJson);
			if (snapshot.Version != snapshotVersion)
				return RecoveryRequired(record, "SnapshotVersionMismatch");
		}
		catch (InvalidDataException)
		{
			return RecoveryRequired(record, "SnapshotFormatInvalid", recoverySessionKey);
		}

		return GameProgressLoadResult.Loaded(new SavedGameSession(record, snapshotVersion, snapshotJson));
	}

	private GameProgressWriteResult ReplaceActive(
		SavedGameSession replacement,
		string? replacedSessionKey,
		DateTimeOffset abandonedAtUtc)
	{
		if (!IsValidActiveSession(replacement, out var reason))
			return GameProgressWriteResult.Failed(reason);
		if (abandonedAtUtc.Offset != TimeSpan.Zero)
			return GameProgressWriteResult.Failed("AbandonedTimestampMustBeUtc");
		if (replacedSessionKey is not null &&
			string.Equals(replacedSessionKey, replacement.Record.SessionId.ToString("D"), StringComparison.OrdinalIgnoreCase))
			return GameProgressWriteResult.Conflict("ReplacementSessionMustHaveANewId");

		try
		{
			using var connection = OpenWriter();
			using var transaction = connection.BeginTransaction();
			if (replacedSessionKey is not null)
			{
				using var abandon = connection.CreateCommand();
				abandon.Transaction = transaction;
				abandon.CommandText = """
					UPDATE GameSessions
					SET Status = 'Abandoned', CompletedAtUtc = NULL, AbandonedAtUtc = $abandoned,
						SnapshotVersion = NULL, SnapshotJson = NULL
					WHERE SessionId = $id AND Status = 'Active';
					""";
				abandon.Parameters.AddWithValue("$id", replacedSessionKey);
				abandon.Parameters.AddWithValue("$abandoned", FormatUtc(abandonedAtUtc));
				if (abandon.ExecuteNonQuery() != 1)
				{
					transaction.Rollback();
					return GameProgressWriteResult.Conflict("ActiveSessionNotFound");
				}
			}

			using var insert = connection.CreateCommand();
			insert.Transaction = transaction;
			insert.CommandText =
				"INSERT INTO GameSessions (" + SessionColumns + ") VALUES ($id, $status, $started, NULL, NULL, $difficulty, $elapsed, $errors, $hints, $snapshotVersion, $snapshotJson);";
			AddRecordParameters(insert, replacement.Record);
			insert.Parameters.AddWithValue("$snapshotVersion", replacement.SnapshotVersion);
			insert.Parameters.AddWithValue("$snapshotJson", replacement.SnapshotJson);
			insert.ExecuteNonQuery();
			transaction.Commit();
			return GameProgressWriteResult.Saved();
		}
		catch (SqliteException exception)
		{
			LogWriteFailure("ReplaceActive", replacement.SessionId, exception);
			return IsUniqueConstraintViolation(exception)
				? GameProgressWriteResult.Conflict("ActiveSessionAlreadyExists")
				: GameProgressWriteResult.Failed("DatabaseWriteFailed");
		}
	}

	private GameProgressWriteResult Save(SavedGameSession session)
	{
		if (!IsValidActiveSession(session, out var reason))
			return GameProgressWriteResult.Failed(reason);

		try
		{
			using var connection = OpenWriter();
			using var transaction = connection.BeginTransaction();
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
				UPDATE GameSessions
				SET StartedAtUtc = $started,
					Difficulty = $difficulty,
					ActiveElapsedTicks = $elapsed,
					ErrorCount = $errors,
					DisplayedHintLevelCount = $hints,
					SnapshotVersion = $snapshotVersion,
					SnapshotJson = $snapshotJson
				WHERE SessionId = $id AND Status = 'Active';
				""";
			AddRecordParameters(command, session.Record);
			command.Parameters.AddWithValue("$snapshotVersion", session.SnapshotVersion);
			command.Parameters.AddWithValue("$snapshotJson", session.SnapshotJson);
			if (command.ExecuteNonQuery() != 1)
			{
				transaction.Rollback();
				return GameProgressWriteResult.Conflict("ActiveSessionNotFound");
			}
			transaction.Commit();
			return GameProgressWriteResult.Saved();
		}
		catch (SqliteException exception)
		{
			LogWriteFailure("Save", session.SessionId, exception);
			return GameProgressWriteResult.Failed("DatabaseWriteFailed");
		}
	}

	private GameProgressWriteResult TransitionToTerminal(GameProgressRecord record, GameProgressStatus targetStatus)
	{
		if (!IsValidTerminalRecord(record, targetStatus, out var reason))
			return GameProgressWriteResult.Failed(reason);

		try
		{
			using var connection = OpenWriter();
			using var transaction = connection.BeginTransaction();
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = targetStatus == GameProgressStatus.Completed
				? """
					UPDATE GameSessions
					SET Status = 'Completed', CompletedAtUtc = $completed, AbandonedAtUtc = NULL,
						Difficulty = $difficulty, ActiveElapsedTicks = $elapsed, ErrorCount = $errors,
						DisplayedHintLevelCount = $hints, SnapshotVersion = NULL, SnapshotJson = NULL
					WHERE SessionId = $id AND Status = 'Active';
					"""
				: """
					UPDATE GameSessions
					SET Status = 'Abandoned', CompletedAtUtc = NULL, AbandonedAtUtc = $abandoned,
						Difficulty = $difficulty, ActiveElapsedTicks = $elapsed, ErrorCount = $errors,
						DisplayedHintLevelCount = $hints, SnapshotVersion = NULL, SnapshotJson = NULL
					WHERE SessionId = $id AND Status = 'Active';
					""";
			AddRecordParameters(command, record);
			command.Parameters.AddWithValue("$completed", DbValue(record.CompletedAtUtc));
			command.Parameters.AddWithValue("$abandoned", DbValue(record.AbandonedAtUtc));
			if (command.ExecuteNonQuery() == 1)
			{
				transaction.Commit();
				return GameProgressWriteResult.Saved();
			}

			var existing = FindRecord(connection, transaction, record.SessionId);
			transaction.Commit();
			return IsSameTerminalTransition(existing, record, targetStatus)
				? GameProgressWriteResult.Saved()
				: GameProgressWriteResult.Conflict("SessionIsNotActive");
		}
		catch (SqliteException exception)
		{
			LogWriteFailure(targetStatus.ToString(), record.SessionId, exception);
			return GameProgressWriteResult.Failed("DatabaseWriteFailed");
		}
	}

	private GameProgressWriteResult AbandonUnrecoverableActive(string sessionKey, DateTimeOffset abandonedAtUtc)
	{
		if (abandonedAtUtc.Offset != TimeSpan.Zero)
			return GameProgressWriteResult.Failed("SessionAndTimestampMustBeValid");

		try
		{
			using var connection = OpenWriter();
			using var transaction = connection.BeginTransaction();
			using var command = connection.CreateCommand();
			command.Transaction = transaction;
			command.CommandText = """
				UPDATE GameSessions
				SET Status = 'Abandoned', CompletedAtUtc = NULL, AbandonedAtUtc = $abandoned,
					SnapshotVersion = NULL, SnapshotJson = NULL
				WHERE SessionId = $id AND Status = 'Active';
				""";
			command.Parameters.AddWithValue("$id", sessionKey);
			command.Parameters.AddWithValue("$abandoned", FormatUtc(abandonedAtUtc));
			var changed = command.ExecuteNonQuery();
			transaction.Commit();
			return changed == 1 ? GameProgressWriteResult.Saved() : GameProgressWriteResult.Conflict("ActiveSessionNotFound");
		}
		catch (SqliteException exception)
		{
			_logger.LogError(exception,
				"Game progress database operation {Operation} failed for unrecoverable session row {SessionKey} with SQLite code {SqliteErrorCode}.",
				"AbandonUnrecoverableActive",
				sessionKey,
				exception.SqliteErrorCode);
			return GameProgressWriteResult.Failed("DatabaseWriteFailed");
		}
	}

	private static bool IsSameTerminalTransition(
		GameProgressRecord? existing,
		GameProgressRecord expected,
		GameProgressStatus targetStatus) =>
		existing is not null &&
		existing.SessionId == expected.SessionId &&
		existing.Status == targetStatus &&
		existing.StartedAtUtc == expected.StartedAtUtc &&
		existing.Difficulty == expected.Difficulty &&
		existing.ActiveElapsedTicks == expected.ActiveElapsedTicks &&
		existing.ErrorCount == expected.ErrorCount &&
		existing.DisplayedHintLevelCount == expected.DisplayedHintLevelCount &&
		(targetStatus == GameProgressStatus.Completed
			? existing.CompletedAtUtc.HasValue && !existing.AbandonedAtUtc.HasValue
			: existing.AbandonedAtUtc.HasValue && !existing.CompletedAtUtc.HasValue);

	private IReadOnlyList<GameProgressRecord> GetRecords()
	{
		using var connection = OpenReader();
		using var command = connection.CreateCommand();
		command.CommandText = $"SELECT {RecordColumns} FROM GameSessions ORDER BY StartedAtUtc, SessionId;";
		using var reader = command.ExecuteReader();
		var records = new List<GameProgressRecord>();
		while (reader.Read())
		{
			try
			{
				records.Add(ReadRecord(reader));
			}
			catch (Exception exception) when (IsInvalidPersistedRecord(exception))
			{
				_logger.LogWarning(exception,
					"Game progress row {SessionKey} was skipped because it contains invalid persisted fields.",
					GetRawSessionKey(reader));
			}
		}
		return records.AsReadOnly();
	}

	private SavedGameSession? FindSession(SqliteConnection connection, SqliteTransaction transaction, Guid sessionId)
	{
		using var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = $"SELECT {SessionColumns} FROM GameSessions WHERE SessionId = $id;";
		command.Parameters.AddWithValue("$id", sessionId.ToString("D"));
		using var reader = command.ExecuteReader();
		if (!reader.Read())
			return null;
		var record = ReadRecord(reader);
		if (reader.IsDBNull(9) || reader.IsDBNull(10))
			return null;
		return new SavedGameSession(record, reader.GetInt32(9), reader.GetString(10));
	}

	private GameProgressRecord? FindRecord(SqliteConnection connection, SqliteTransaction transaction, Guid sessionId)
	{
		using var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = $"SELECT {RecordColumns} FROM GameSessions WHERE SessionId = $id;";
		command.Parameters.AddWithValue("$id", sessionId.ToString("D"));
		using var reader = command.ExecuteReader();
		return reader.Read() ? ReadRecord(reader) : null;
	}

	private static GameProgressRecord ReadRecord(SqliteDataReader reader)
	{
		if (!Guid.TryParse(reader.GetString(0), out var sessionId) || sessionId == Guid.Empty)
			throw new InvalidDataException("The database contains an invalid game session identifier.");
		if (!Enum.TryParse<GameProgressStatus>(reader.GetString(1), ignoreCase: false, out var status) || !Enum.IsDefined(status))
			throw new InvalidDataException("The database contains an unknown game progress status.");
		var difficulty = (DifficultyLevel)reader.GetInt32(5);
		if (!Enum.IsDefined(difficulty))
			throw new InvalidDataException("The database contains an unknown game difficulty.");
		var record = new GameProgressRecord(
			sessionId,
			status,
			ParseUtc(reader.GetString(2)),
			reader.IsDBNull(3) ? null : ParseUtc(reader.GetString(3)),
			reader.IsDBNull(4) ? null : ParseUtc(reader.GetString(4)),
			difficulty,
			reader.GetInt64(6),
			reader.GetInt32(7),
			reader.GetInt32(8));
		if (!IsValidRecord(record))
			throw new InvalidDataException("The database contains invalid game progress metrics or timestamps.");
		return record;
	}

	private static void AddRecordParameters(SqliteCommand command, GameProgressRecord record)
	{
		command.Parameters.AddWithValue("$id", record.SessionId.ToString("D"));
		command.Parameters.AddWithValue("$status", record.Status.ToString());
		command.Parameters.AddWithValue("$started", FormatUtc(record.StartedAtUtc));
		command.Parameters.AddWithValue("$difficulty", (int)record.Difficulty);
		command.Parameters.AddWithValue("$elapsed", record.ActiveElapsedTicks);
		command.Parameters.AddWithValue("$errors", record.ErrorCount);
		command.Parameters.AddWithValue("$hints", record.DisplayedHintLevelCount);
	}

	private static bool IsValidActiveSession(SavedGameSession? session, out string reason)
	{
		if (session is null || session.Record is null || session.Record.Status != GameProgressStatus.Active ||
			session.Record.SessionId == Guid.Empty || session.Record.CompletedAtUtc.HasValue || session.Record.AbandonedAtUtc.HasValue ||
			!IsValidRecord(session.Record) || session.SnapshotVersion != GameSessionPersistenceSnapshot.CurrentVersion ||
			string.IsNullOrWhiteSpace(session.SnapshotJson))
		{
			reason = "ActiveSessionInvalid";
			return false;
		}

		try
		{
			if (SnapshotMapper.Deserialize(session.SnapshotJson).Version != session.SnapshotVersion)
			{
				reason = "SnapshotVersionMismatch";
				return false;
			}
		}
		catch (InvalidDataException)
		{
			reason = "SnapshotFormatInvalid";
			return false;
		}

		reason = string.Empty;
		return true;
	}

	private static bool IsValidTerminalRecord(GameProgressRecord? record, GameProgressStatus expectedStatus, out string reason)
	{
		var validTimestamps = expectedStatus == GameProgressStatus.Completed
			? record?.CompletedAtUtc is not null && record.AbandonedAtUtc is null
			: record?.AbandonedAtUtc is not null && record.CompletedAtUtc is null;
		if (record is null || record.SessionId == Guid.Empty || record.Status != expectedStatus || !validTimestamps || !IsValidRecord(record))
		{
			reason = "TerminalRecordInvalid";
			return false;
		}

		reason = string.Empty;
		return true;
	}

	private static bool IsValidRecord(GameProgressRecord record)
	{
		var terminalTimestampsMatchStatus = record.Status switch
		{
			GameProgressStatus.Active => !record.CompletedAtUtc.HasValue && !record.AbandonedAtUtc.HasValue,
			GameProgressStatus.Completed => record.CompletedAtUtc.HasValue && !record.AbandonedAtUtc.HasValue,
			GameProgressStatus.Abandoned => !record.CompletedAtUtc.HasValue && record.AbandonedAtUtc.HasValue,
			_ => false
		};
		var terminalTimesFollowStart =
			(!record.CompletedAtUtc.HasValue || record.CompletedAtUtc.Value >= record.StartedAtUtc) &&
			(!record.AbandonedAtUtc.HasValue || record.AbandonedAtUtc.Value >= record.StartedAtUtc);

		return Enum.IsDefined(record.Status) && Enum.IsDefined(record.Difficulty) &&
			record.StartedAtUtc.Offset == TimeSpan.Zero &&
			(!record.CompletedAtUtc.HasValue || record.CompletedAtUtc.Value.Offset == TimeSpan.Zero) &&
			(!record.AbandonedAtUtc.HasValue || record.AbandonedAtUtc.Value.Offset == TimeSpan.Zero) &&
			terminalTimestampsMatchStatus && terminalTimesFollowStart &&
			record.ActiveElapsedTicks >= 0 && record.ErrorCount >= 0 && record.DisplayedHintLevelCount >= 0;
	}

	private SqliteConnection OpenReader()
	{
		var connection = _connectionFactory.CreateConnection();
		connection.Open();
		return connection;
	}

	private SqliteConnection OpenWriter()
	{
		var connection = _connectionFactory.CreateConnection();
		connection.Open();
		try
		{
			using var journal = connection.CreateCommand();
			journal.CommandText = "PRAGMA journal_mode = WAL;";
			journal.ExecuteScalar();
			using var synchronous = connection.CreateCommand();
			synchronous.CommandText = "PRAGMA synchronous = FULL;";
			synchronous.ExecuteNonQuery();
			using var foreignKeys = connection.CreateCommand();
			foreignKeys.CommandText = "PRAGMA foreign_keys = ON;";
			foreignKeys.ExecuteNonQuery();
			return connection;
		}
		catch
		{
			connection.Dispose();
			throw;
		}
	}

	private static DateTimeOffset ParseUtc(string value) =>
		DateTimeOffset.ParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

	private static string FormatUtc(DateTimeOffset value) =>
		value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

	private static object DbValue(DateTimeOffset? value) => value.HasValue ? FormatUtc(value.Value) : DBNull.Value;

	private GameProgressLoadResult RecoveryRequired(GameProgressRecord? record, string reason, string? recoverySessionKey = null)
	{
		_logger.LogWarning("Game progress recovery is required for session {SessionId}; reason {ReasonCode}.",
			recoverySessionKey ?? record?.SessionId.ToString("D"), reason);
		return GameProgressLoadResult.RecoveryRequired(reason, record, recoverySessionKey);
	}

	private void LogWriteFailure(string operation, Guid sessionId, SqliteException exception)
	{
		_logger.LogError(exception,
			"Game progress database operation {Operation} failed for session {SessionId} with SQLite code {SqliteErrorCode}.",
			operation,
			sessionId,
			exception.SqliteErrorCode);
	}

	private Task<TResult> RunInBackground<TResult>(Func<TResult> operation, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.Run(() =>
		{
			EnsureSchemaMigrated();
			return operation();
		}, cancellationToken);
	}

	private void EnsureSchemaMigrated()
	{
		lock (_schemaMigrationLock)
		{
			if (_schemaMigrated)
				return;

			new SqliteSchemaMigrator(_connectionFactory).Migrate();
			_schemaMigrated = true;
		}
	}

	private static bool IsInvalidPersistedRecord(Exception exception) =>
		exception is InvalidDataException or FormatException or ArgumentException or InvalidCastException or OverflowException;

	private static bool IsUniqueConstraintViolation(SqliteException exception) =>
		exception.SqliteErrorCode == 19 && exception.SqliteExtendedErrorCode is 1555 or 2067;

	private static string? GetRawSessionKey(SqliteDataReader reader) =>
		reader.IsDBNull(0) ? null : Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture);

}
