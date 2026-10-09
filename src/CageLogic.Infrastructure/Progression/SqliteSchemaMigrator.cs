using Microsoft.Data.Sqlite;

namespace CageLogic.Infrastructure.Progression;

/// <summary>Applies ordered, transactional schema migrations to the progression database.</summary>
public sealed class SqliteSchemaMigrator(SqliteConnectionFactory connectionFactory)
{
	private const int CurrentSchemaVersion = 1;

	public void Migrate()
	{
		using var connection = connectionFactory.CreateConnection();
		connection.Open();

		using var transaction = connection.BeginTransaction();
		var version = ReadSchemaVersion(connection, transaction);
		if (version > CurrentSchemaVersion)
			throw new InvalidOperationException($"Database schema version {version} is newer than supported version {CurrentSchemaVersion}.");

		if (version == CurrentSchemaVersion)
		{
			transaction.Commit();
			return;
		}

		if (version != 0)
			throw new InvalidOperationException($"Database schema version {version} is not supported.");

		ApplyVersionOne(connection, transaction);
		SetSchemaVersion(connection, transaction, CurrentSchemaVersion);
		transaction.Commit();
	}

	private static void ApplyVersionOne(SqliteConnection connection, SqliteTransaction transaction)
	{
		Execute(connection, transaction,
			"""
			CREATE TABLE GameSessions (
				SessionId TEXT NOT NULL PRIMARY KEY,
				Status TEXT NOT NULL CHECK (Status IN ('Active', 'Completed', 'Abandoned')),
				StartedAtUtc TEXT NOT NULL,
				CompletedAtUtc TEXT NULL,
				AbandonedAtUtc TEXT NULL,
				Difficulty INTEGER NOT NULL,
				ActiveElapsedTicks INTEGER NOT NULL CHECK (ActiveElapsedTicks >= 0),
				ErrorCount INTEGER NOT NULL CHECK (ErrorCount >= 0),
				DisplayedHintLevelCount INTEGER NOT NULL CHECK (DisplayedHintLevelCount >= 0),
				SnapshotVersion INTEGER NULL CHECK (SnapshotVersion IS NULL OR SnapshotVersion > 0),
				SnapshotJson TEXT NULL,
				CHECK (
					(Status = 'Active' AND CompletedAtUtc IS NULL AND AbandonedAtUtc IS NULL AND SnapshotVersion IS NOT NULL AND SnapshotJson IS NOT NULL)
					OR (Status = 'Completed' AND CompletedAtUtc IS NOT NULL AND AbandonedAtUtc IS NULL AND SnapshotVersion IS NULL AND SnapshotJson IS NULL)
					OR (Status = 'Abandoned' AND CompletedAtUtc IS NULL AND AbandonedAtUtc IS NOT NULL AND SnapshotVersion IS NULL AND SnapshotJson IS NULL)
				)
			);
			""");
		Execute(connection, transaction,
			"CREATE UNIQUE INDEX UX_GameSessions_Active ON GameSessions(Status) WHERE Status = 'Active';");
		Execute(connection, transaction,
			"CREATE INDEX IX_GameSessions_Status_StartedAtUtc ON GameSessions(Status, StartedAtUtc);");
	}

	private static long ReadSchemaVersion(SqliteConnection connection, SqliteTransaction transaction)
	{
		using var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = "PRAGMA user_version;";
		return (long)command.ExecuteScalar()!;
	}

	private static void SetSchemaVersion(SqliteConnection connection, SqliteTransaction transaction, int version)
	{
		using var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = $"PRAGMA user_version = {version};";
		command.ExecuteNonQuery();
	}

	private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
	{
		using var command = connection.CreateCommand();
		command.Transaction = transaction;
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}
}
