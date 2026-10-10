using CageLogic.Infrastructure.Progression;
using Microsoft.Data.Sqlite;

namespace CageLogic.Infrastructure.Tests.Progression;

public sealed class ProgressionSchemaTests
{
	[Test]
	public void Migrate_CreatesVersionOneSchemaAndRequiredColumns()
	{
		WithDatabase((factory, path) =>
		{
			var migrator = new SqliteSchemaMigrator(factory);
			migrator.Migrate();

			using var connection = Open(path);
			Assert.That(ReadInt64(connection, "PRAGMA user_version;"), Is.EqualTo(1));

			using var command = connection.CreateCommand();
			command.CommandText = "PRAGMA table_info('GameSessions');";
			using var reader = command.ExecuteReader();
			var columns = new Dictionary<string, (string Type, bool NotNull, bool PrimaryKey)>();
			while (reader.Read())
				columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt32(3) == 1, reader.GetInt32(5) == 1));

			Assert.That(columns.Keys, Is.EquivalentTo(new[]
			{
				"SessionId", "Status", "StartedAtUtc", "CompletedAtUtc", "AbandonedAtUtc", "Difficulty",
				"ActiveElapsedTicks", "ErrorCount", "DisplayedHintLevelCount", "SnapshotVersion", "SnapshotJson"
			}));
			Assert.That(columns["SessionId"].PrimaryKey, Is.True);
			Assert.That(columns["Status"].NotNull, Is.True);
			Assert.That(columns["StartedAtUtc"].NotNull, Is.True);
			Assert.That(columns["Difficulty"].NotNull, Is.True);
		});
	}

	[Test]
	public void Migrate_IsIdempotentAndCreatesOnlyOneActiveSessionIndex()
	{
		WithDatabase((factory, path) =>
		{
			var migrator = new SqliteSchemaMigrator(factory);
			migrator.Migrate();
			migrator.Migrate();

			using var connection = Open(path);
			Assert.That(ReadInt64(connection, "PRAGMA user_version;"), Is.EqualTo(1));
			Assert.That(ReadInt64(connection,
				"SELECT COUNT(*) FROM sqlite_master WHERE type = 'index' AND name = 'UX_GameSessions_Active';"), Is.EqualTo(1));

			InsertActive(connection, "active-one");
			Assert.Throws<SqliteException>(() => InsertActive(connection, "active-two"));
			InsertCompleted(connection, "completed-one");
		});
	}

	[Test]
	public void Schema_RejectsInvalidStatesTimestampsSnapshotsAndNegativeCounters()
	{
		WithDatabase((factory, path) =>
		{
			new SqliteSchemaMigrator(factory).Migrate();
			using var connection = Open(path);

			var invalidRows = new[]
			{
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('bad-status', 'Paused', '2026-10-09T00:00:00Z', 1, 0, 0, 0, 1, '{}');",
				"INSERT INTO GameSessions (SessionId, Status, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('missing-start', 'Active', 1, 0, 0, 0, 1, '{}');",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('missing-snapshot', 'Active', '2026-10-09T00:00:00Z', 1, 0, 0, 0, NULL, NULL);",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, CompletedAtUtc, SnapshotVersion, SnapshotJson) VALUES ('completed-without-time', 'Completed', '2026-10-09T00:00:00Z', 1, 0, 0, 0, NULL, NULL, NULL);",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, AbandonedAtUtc, SnapshotVersion, SnapshotJson) VALUES ('abandoned-without-time', 'Abandoned', '2026-10-09T00:00:00Z', 1, 0, 0, 0, NULL, NULL, NULL);",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, CompletedAtUtc, AbandonedAtUtc, SnapshotVersion, SnapshotJson) VALUES ('wrong-terminal-time', 'Completed', '2026-10-09T00:00:00Z', 1, 0, 0, 0, '2026-10-09T00:01:00Z', '2026-10-09T00:01:00Z', NULL, NULL);",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('negative-time', 'Active', '2026-10-09T00:00:00Z', 1, -1, 0, 0, 1, '{}');",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('negative-errors', 'Active', '2026-10-09T00:00:00Z', 1, 0, -1, 0, 1, '{}');",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('negative-hints', 'Active', '2026-10-09T00:00:00Z', 1, 0, 0, -1, 1, '{}');",
				"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, CompletedAtUtc, SnapshotVersion, SnapshotJson) VALUES ('terminal-snapshot', 'Completed', '2026-10-09T00:00:00Z', 1, 0, 0, 0, '2026-10-09T00:01:00Z', 1, '{}');"
			};

			foreach (var invalidRow in invalidRows)
				Assert.Throws<SqliteException>(() => Execute(connection, invalidRow), invalidRow);
		});
	}

	[Test]
	public void Migrate_RollsBackVersionAndNewSchemaWhenVersionOneMigrationFails()
	{
		WithDatabase((factory, path) =>
		{
			using (var connection = Open(path))
			{
				Execute(connection, "CREATE TABLE ExistingValidRecords (RecordId TEXT PRIMARY KEY, Value TEXT NOT NULL);");
				Execute(connection, "INSERT INTO ExistingValidRecords VALUES ('valid-1', 'preserve-me');");
				Execute(connection, "CREATE TABLE UX_GameSessions_Active (BlockerId INTEGER PRIMARY KEY);");
			}

			Assert.Throws<SqliteException>(() => new SqliteSchemaMigrator(factory).Migrate());

			using var verification = Open(path);
			Assert.That(ReadInt64(verification, "PRAGMA user_version;"), Is.Zero);
			Assert.That(ReadInt64(verification,
				"SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'GameSessions';"), Is.Zero);
			Assert.That(ReadString(verification, "SELECT Value FROM ExistingValidRecords WHERE RecordId = 'valid-1';"), Is.EqualTo("preserve-me"));
		});
	}

	[Test]
	public void Migrate_RejectsUnknownFutureSchemaVersionWithoutRewritingIt()
	{
		WithDatabase((factory, path) =>
		{
			using (var connection = Open(path))
				Execute(connection, "PRAGMA user_version = 42;");

			Assert.Throws<InvalidOperationException>(() => new SqliteSchemaMigrator(factory).Migrate());

			using var verification = Open(path);
			Assert.That(ReadInt64(verification, "PRAGMA user_version;"), Is.EqualTo(42));
		});
	}

	private static void InsertActive(SqliteConnection connection, string id)
	{
		Execute(connection, $"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount, SnapshotVersion, SnapshotJson) VALUES ('{id}', 'Active', '2026-10-09T00:00:00Z', 1, 0, 0, 0, 1, '{{}}');");
	}

	private static void InsertCompleted(SqliteConnection connection, string id)
	{
		Execute(connection, $"INSERT INTO GameSessions (SessionId, Status, StartedAtUtc, CompletedAtUtc, Difficulty, ActiveElapsedTicks, ErrorCount, DisplayedHintLevelCount) VALUES ('{id}', 'Completed', '2026-10-09T00:00:00Z', '2026-10-09T00:01:00Z', 1, 0, 0, 0);");
	}

	private static void Execute(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	private static long ReadInt64(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return (long)command.ExecuteScalar()!;
	}

	private static string ReadString(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		return (string)command.ExecuteScalar()!;
	}

	private static SqliteConnection Open(string path)
	{
		var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
		connection.Open();
		return connection;
	}

	private static void WithDatabase(Action<SqliteConnectionFactory, string> action)
	{
		var directory = Path.Combine(Path.GetTempPath(), $"CageLogic.Progression.Schema.{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, "progression.db");
		try
		{
			action(new SqliteConnectionFactory(path), path);
		}
		finally
		{
			SqliteConnection.ClearAllPools();
			Directory.Delete(directory, recursive: true);
		}
	}
}
