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
		Guid sessionId,
		DateTimeOffset abandonedAtUtc,
		CancellationToken cancellationToken = default) =>
		RunInBackground(() => AbandonUnrecoverableActive(sessionId, abandonedAtUtc), cancellationToken);

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
			return exception.SqliteErrorCode is 19 or 2067
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

		var record = ReadRecord(reader);
		if (reader.IsDBNull(9) || reader.IsDBNull(10))
			return RecoveryRequired(record, "ActiveSnapshotMissing");

		var snapshotVersion = reader.GetInt32(9);
		var snapshotJson = reader.GetString(10);
		if (snapshotVersion != GameSessionPersistenceSnapshot.CurrentVersion)
			return RecoveryRequired(record, "SnapshotVersionUnsupported");

		try
		{
			var snapshot = SnapshotMapper.Deserialize(snapshotJson);
			if (snapshot.Version != snapshotVersion)
				return RecoveryRequired(record, "SnapshotVersionMismatch");
		}
		catch (InvalidDataException)
		{
			return RecoveryRequired(record, "SnapshotFormatInvalid");
		}

		return GameProgressLoadResult.Loaded(new SavedGameSession(record, snapshotVersion, snapshotJson));
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

	private GameProgressWriteResult AbandonUnrecoverableActive(Guid sessionId, DateTimeOffset abandonedAtUtc)
	{
		if (sessionId == Guid.Empty || abandonedAtUtc.Offset != TimeSpan.Zero)
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
			command.Parameters.AddWithValue("$id", sessionId.ToString("D"));
			command.Parameters.AddWithValue("$abandoned", FormatUtc(abandonedAtUtc));
			var changed = command.ExecuteNonQuery();
			transaction.Commit();
			return changed == 1 ? GameProgressWriteResult.Saved() : GameProgressWriteResult.Conflict("ActiveSessionNotFound");
		}
		catch (SqliteException exception)
		{
			LogWriteFailure("AbandonUnrecoverableActive", sessionId, exception);
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
			records.Add(ReadRecord(reader));
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
		var sessionId = Guid.Parse(reader.GetString(0));
		if (!Enum.TryParse<GameProgressStatus>(reader.GetString(1), ignoreCase: false, out var status) || !Enum.IsDefined(status))
			throw new InvalidDataException("The database contains an unknown game progress status.");
		var difficulty = (DifficultyLevel)reader.GetInt32(5);
		if (!Enum.IsDefined(difficulty))
			throw new InvalidDataException("The database contains an unknown game difficulty.");
		return new GameProgressRecord(
			sessionId,
			status,
			ParseUtc(reader.GetString(2)),
			reader.IsDBNull(3) ? null : ParseUtc(reader.GetString(3)),
			reader.IsDBNull(4) ? null : ParseUtc(reader.GetString(4)),
			difficulty,
			reader.GetInt64(6),
			reader.GetInt32(7),
			reader.GetInt32(8));
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

	private static bool IsValidRecord(GameProgressRecord record) =>
		Enum.IsDefined(record.Status) && Enum.IsDefined(record.Difficulty) &&
		record.StartedAtUtc.Offset == TimeSpan.Zero &&
		(!record.CompletedAtUtc.HasValue || record.CompletedAtUtc.Value.Offset == TimeSpan.Zero) &&
		(!record.AbandonedAtUtc.HasValue || record.AbandonedAtUtc.Value.Offset == TimeSpan.Zero) &&
		record.ActiveElapsedTicks >= 0 && record.ErrorCount >= 0 && record.DisplayedHintLevelCount >= 0;

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

	private GameProgressLoadResult RecoveryRequired(GameProgressRecord record, string reason)
	{
		_logger.LogWarning("Game progress recovery is required for session {SessionId}; reason {ReasonCode}.", record.SessionId, reason);
		return GameProgressLoadResult.RecoveryRequired(reason, record);
	}

	private void LogWriteFailure(string operation, Guid sessionId, SqliteException exception)
	{
		_logger.LogError(exception,
			"Game progress database operation {Operation} failed for session {SessionId} with SQLite code {SqliteErrorCode}.",
			operation,
			sessionId,
			exception.SqliteErrorCode);
	}

	private static Task<TResult> RunInBackground<TResult>(Func<TResult> operation, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		return Task.Run(operation, cancellationToken);
	}

}
