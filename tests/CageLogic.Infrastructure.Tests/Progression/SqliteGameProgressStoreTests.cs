using CageLogic.Application.Difficulty;
using CageLogic.Application.Progression;
using CageLogic.Infrastructure.Progression;
using Microsoft.Data.Sqlite;

namespace CageLogic.Infrastructure.Tests.Progression;

public sealed class SqliteGameProgressStoreTests
{
	[Test]
	public async Task CreateAndLoadActiveSession_PersistSnapshotAndMetricsAtomically()
	{
		await WithStoreAsync(async (factory, store, path) =>
		{
			var created = ActiveSession(snapshot: "{\"version\":1}", elapsedTicks: 17, errors: 2, hints: 3);
			Assert.That((await store.CreateAsync(created)).IsSaved, Is.True);

			var loaded = await store.LoadActiveAsync();

			Assert.That(loaded.Status, Is.EqualTo(GameProgressLoadStatus.Loaded));
			Assert.That(loaded.Session, Is.EqualTo(created));
			var records = await store.GetRecordsAsync();
			Assert.That(records, Has.Count.EqualTo(1));
			Assert.That(records[0].ErrorCount, Is.EqualTo(2));
			Assert.That(records[0].DisplayedHintLevelCount, Is.EqualTo(3));
		});
	}

	[Test]
	public async Task SaveFailure_RollsBackSnapshotAndKeepsLastValidSession()
	{
		await WithStoreAsync(async (factory, store, path) =>
		{
			var original = ActiveSession(snapshot: "{\"version\":1,\"value\":\"original\"}", elapsedTicks: 12);
			Assert.That((await store.CreateAsync(original)).IsSaved, Is.True);
			using (var connection = Open(path))
				Execute(connection,
					"CREATE TRIGGER FailProgressSave BEFORE UPDATE OF SnapshotJson ON GameSessions WHEN NEW.SnapshotJson = '{\"version\":1,\"value\":\"trigger-fail\"}' BEGIN SELECT RAISE(ABORT, 'injected write failure'); END;");

			var failed = await store.SaveAsync(original with
			{
				Record = original.Record with { ActiveElapsedTicks = 99, ErrorCount = 9 },
				SnapshotJson = "{\"version\":1,\"value\":\"trigger-fail\"}"
			});

			Assert.That(failed.Status, Is.EqualTo(GameProgressWriteStatus.Failed));
			var loaded = await store.LoadActiveAsync();
			Assert.That(loaded.Session, Is.EqualTo(original));
		});
	}

	[TestCase("not-json", GameProgressLoadStatus.RecoveryRequired)]
	[TestCase("{\"version\":99}", GameProgressLoadStatus.RecoveryRequired)]
	public async Task LoadActive_InvalidOrUnknownSnapshotVersionRequiresRecovery(string json, GameProgressLoadStatus expected)
	{
		await WithStoreAsync(async (factory, store, path) =>
		{
			var active = ActiveSession(snapshot: "{\"version\":1}");
			Assert.That((await store.CreateAsync(active)).IsSaved, Is.True);
			using (var connection = Open(path))
			{
				using var command = connection.CreateCommand();
				command.CommandText = "UPDATE GameSessions SET SnapshotJson = $snapshot, SnapshotVersion = 1 WHERE SessionId = $id;";
				command.Parameters.AddWithValue("$snapshot", json);
				command.Parameters.AddWithValue("$id", active.SessionId.ToString("D"));
				command.ExecuteNonQuery();
			}

			var result = await store.LoadActiveAsync();

			Assert.That(result.Status, Is.EqualTo(expected));
			Assert.That(result.Session, Is.Null);
			Assert.That(result.Record?.SessionId, Is.EqualTo(active.SessionId));
		});
	}

	[Test]
	public async Task AbandonUnrecoverableActive_ArchivesMetricsAndClearsUnreadableSnapshot()
	{
		await WithStoreAsync(async (factory, store, path) =>
		{
			var active = ActiveSession(elapsedTicks: 19, errors: 2, hints: 3);
			Assert.That((await store.CreateAsync(active)).IsSaved, Is.True);
			using (var connection = Open(path))
				Execute(connection, $"UPDATE GameSessions SET SnapshotJson = 'corrupt' WHERE SessionId = '{active.SessionId:D}';");

			var recovery = await store.LoadActiveAsync();
			var abandonedAt = active.Record.StartedAtUtc.AddMinutes(5);
			Assert.That(recovery.Status, Is.EqualTo(GameProgressLoadStatus.RecoveryRequired));
			Assert.That((await store.AbandonUnrecoverableActiveAsync(active.SessionId, abandonedAt)).IsSaved, Is.True);

			Assert.That((await store.LoadActiveAsync()).Status, Is.EqualTo(GameProgressLoadStatus.NoActiveSession));
			var record = (await store.GetRecordsAsync()).Single();
			Assert.That(record.Status, Is.EqualTo(GameProgressStatus.Abandoned));
			Assert.That(record.AbandonedAtUtc, Is.EqualTo(abandonedAt));
			Assert.That(record.ErrorCount, Is.EqualTo(2));
			Assert.That(record.DisplayedHintLevelCount, Is.EqualTo(3));
			using var inspect = Open(path);
			Assert.That(ReadScalar(inspect, "SELECT SnapshotJson FROM GameSessions WHERE SessionId = $id;", active.SessionId), Is.Null);
		});
	}

	[Test]
	public async Task AbandonUnrecoverableActive_RequiresTheExpectedSessionId()
	{
		await WithStoreAsync(async (_, store, _) =>
		{
			var active = ActiveSession();
			Assert.That((await store.CreateAsync(active)).IsSaved, Is.True);

			var result = await store.AbandonUnrecoverableActiveAsync(Guid.NewGuid(), active.Record.StartedAtUtc.AddMinutes(1));

			Assert.That(result.Status, Is.EqualTo(GameProgressWriteStatus.Conflict));
			Assert.That((await store.LoadActiveAsync()).Session, Is.EqualTo(active));
		});
	}

	[Test]
	public async Task Create_EnforcesSingleActiveSession_AndKeepsFirstSnapshot()
	{
		await WithStoreAsync(async (_, store, path) =>
		{
			var first = ActiveSession(snapshot: "{\"version\":1,\"id\":1}");
			Assert.That((await store.CreateAsync(first)).IsSaved, Is.True);
			var second = ActiveSession(Guid.NewGuid(), "{\"version\":1,\"id\":2}");

			var rejected = await store.CreateAsync(second);

			Assert.That(rejected.Status, Is.EqualTo(GameProgressWriteStatus.Conflict));
			Assert.That((await store.LoadActiveAsync()).Session, Is.EqualTo(first));
			Assert.That(await store.GetRecordsAsync(), Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task CreateRetry_ForSameActiveSessionUpdatesSnapshotAtomically()
	{
		await WithStoreAsync(async (_, store, _) =>
		{
			var first = ActiveSession(snapshot: "{\"version\":1,\"value\":1}", elapsedTicks: 1);
			var retry = first with
			{
				Record = first.Record with { ActiveElapsedTicks = 2, ErrorCount = 1 },
				SnapshotJson = "{\"version\":1,\"value\":2}"
			};
			Assert.That((await store.CreateAsync(first)).IsSaved, Is.True);

			Assert.That((await store.CreateAsync(retry)).IsSaved, Is.True);
			Assert.That((await store.LoadActiveAsync()).Session, Is.EqualTo(retry));
		});
	}

	[Test]
	public async Task Complete_IsIdempotentAndClearsSnapshotFromTerminalRecord()
	{
		await WithStoreAsync(async (_, store, path) =>
		{
			var active = ActiveSession(snapshot: "{\"version\":1}");
			Assert.That((await store.CreateAsync(active)).IsSaved, Is.True);
			var completed = active.Record with
			{
				Status = GameProgressStatus.Completed,
				CompletedAtUtc = active.Record.StartedAtUtc.AddMinutes(4),
				ActiveElapsedTicks = TimeSpan.FromMinutes(4).Ticks,
				ErrorCount = 2,
				DisplayedHintLevelCount = 1
			};

			Assert.That((await store.CompleteAsync(completed)).IsSaved, Is.True);
			Assert.That((await store.CompleteAsync(completed with
			{
				CompletedAtUtc = completed.CompletedAtUtc!.Value.AddMinutes(1)
			})).IsSaved, Is.True);
			Assert.That((await store.LoadActiveAsync()).Status, Is.EqualTo(GameProgressLoadStatus.NoActiveSession));
			var rows = await store.GetRecordsAsync();
			Assert.That(rows, Has.Count.EqualTo(1));
			Assert.That(rows[0], Is.EqualTo(completed));
			using var connection = Open(path);
			Assert.That(ReadScalar(connection, "SELECT SnapshotJson FROM GameSessions WHERE SessionId = $id;", active.SessionId), Is.Null);
		});
	}

	private static SavedGameSession ActiveSession(
		Guid? id = null,
		string snapshot = "{\"version\":1}",
		long elapsedTicks = 0,
		int errors = 0,
		int hints = 0)
	{
		var started = new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero);
		return new SavedGameSession(
			new GameProgressRecord(
				id ?? Guid.NewGuid(),
				GameProgressStatus.Active,
				started,
				null,
				null,
				DifficultyLevel.Easy,
				elapsedTicks,
				errors,
				hints),
			1,
			snapshot);
	}

	private static async Task WithStoreAsync(Func<SqliteConnectionFactory, SqliteGameProgressStore, string, Task> action)
	{
		var directory = Path.Combine(Path.GetTempPath(), $"CageLogic.Progression.Store.{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, "progression.db");
		try
		{
			var factory = new SqliteConnectionFactory(path);
			new SqliteSchemaMigrator(factory).Migrate();
			await action(factory, new SqliteGameProgressStore(factory), path);
		}
		finally
		{
			SqliteConnection.ClearAllPools();
			Directory.Delete(directory, recursive: true);
		}
	}

	private static SqliteConnection Open(string path)
	{
		var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
		connection.Open();
		return connection;
	}

	private static void Execute(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}

	private static object? ReadScalar(SqliteConnection connection, string sql, Guid id)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.Parameters.AddWithValue("$id", id.ToString("D"));
		var value = command.ExecuteScalar();
		return value is DBNull ? null : value;
	}
}
