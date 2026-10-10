using CageLogic.Application.Difficulty;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Generation;
using CageLogic.Application.Progression;
using CageLogic.Application.Tests.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.Progression;

public sealed class GameProgressLifecycleTests
{
	[Test]
	public async Task Create_StoresAnActiveRecordAndReturnsTheSession()
	{
		var store = new FakeGameProgressStore();
		var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
		var useCase = new CreateGameProgressUseCase(store, time);

		var result = await useCase.ExecuteAsync(GameSessionTestData.CreateGeneratedPuzzle());

		Assert.That(result.Persistence.IsSaved, Is.True);
		Assert.That(result.Session, Is.Not.Null);
		Assert.That(result.Record?.Status, Is.EqualTo(GameProgressStatus.Active));
		Assert.That(result.Record?.StartedAtUtc, Is.EqualTo(time.GetUtcNow()));
		Assert.That(store.Active?.SnapshotVersion, Is.EqualTo(GameSessionPersistenceSnapshot.CurrentVersion));
		Assert.That(store.Active?.SnapshotJson, Does.Contain("\"solution\""));
	}

	[Test]
	public async Task CreateFailure_LeavesTheNewSessionAvailableInMemory()
	{
		var store = new FakeGameProgressStore { FailNextWrite = true };
		var result = await new CreateGameProgressUseCase(store).ExecuteAsync(GameSessionTestData.CreateGeneratedPuzzle());

		Assert.That(result.Persistence.IsSaved, Is.False);
		Assert.That(result.Session, Is.Not.Null);
		Assert.That(result.Record?.Status, Is.EqualTo(GameProgressStatus.Active));
		Assert.That(store.Active, Is.Null);
	}

	[Test]
	public async Task Create_CallerCancellationAfterCommitStillReturnsCommittedSession()
	{
		using var cancellation = new CancellationTokenSource();
		var store = new FakeGameProgressStore { AfterCreateCommit = cancellation.Cancel };

		var result = await new CreateGameProgressUseCase(store).ExecuteAsync(
			GameSessionTestData.CreateGeneratedPuzzle(),
			cancellation.Token);

		Assert.That(cancellation.IsCancellationRequested, Is.True);
		Assert.That(result.Persistence.IsSaved, Is.True);
		Assert.That(store.Active?.SessionId, Is.EqualTo(result.Record.SessionId));
	}

	[Test]
	public async Task ReplaceActive_CallerCancellationAfterCommitStillReturnsCommittedSession()
	{
		var store = new FakeGameProgressStore();
		var create = new CreateGameProgressUseCase(store);
		var current = await create.ExecuteAsync(GameSessionTestData.CreateGeneratedPuzzle());
		using var cancellation = new CancellationTokenSource();
		store.AfterReplaceCommit = cancellation.Cancel;

		var replacement = await create.ReplaceActiveAsync(
			GameSessionTestData.CreateGeneratedPuzzle(),
			current.Record.SessionId.ToString("D"),
			cancellation.Token);

		Assert.That(cancellation.IsCancellationRequested, Is.True);
		Assert.That(replacement.Persistence.IsSaved, Is.True);
		Assert.That(store.Active?.SessionId, Is.EqualTo(replacement.Record.SessionId));
		Assert.That(store.Records.Single(record => record.SessionId == current.Record.SessionId).Status,
			Is.EqualTo(GameProgressStatus.Abandoned));
	}

	[Test]
	public async Task SaveAndLoad_RestoreTheSessionPausedWithHistoryAndMetrics()
	{
		var store = new FakeGameProgressStore();
		var created = await new CreateGameProgressUseCase(store).ExecuteAsync(GameSessionTestData.CreateGeneratedPuzzle());
		var session = created.Session!;
		var position = new CellPosition(0, 0);
		session.EnterDigit(position, GameInputMode.Answer, 1);
		session.Undo();
		session.Redo();

		var save = await new SaveGameProgressUseCase(store).ExecuteAsync(session, created.Record!);
		var loaded = await new LoadActiveGameUseCase(store).ExecuteAsync();

		Assert.That(save.IsSaved, Is.True);
		Assert.That(loaded.Status, Is.EqualTo(LoadActiveGameStatus.Loaded));
		Assert.That(loaded.Session?.CurrentBoardSnapshot.GetCell(position).CurrentValue, Is.EqualTo(1));
		Assert.That(loaded.Session?.CanUndo, Is.True);
		Assert.That(loaded.Session?.IsPaused, Is.True);
	}

	[Test]
	public async Task LoadActive_InvalidSnapshotReturnsRecoveryRequired()
	{
		var store = new FakeGameProgressStore
		{
			Active = new SavedGameSession(
				new GameProgressRecord(Guid.NewGuid(), GameProgressStatus.Active,
					new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero), null, null,
					DifficultyLevel.Easy, 0, 0, 0),
				1,
				"broken json")
		};

		var result = await new LoadActiveGameUseCase(store).ExecuteAsync();

		Assert.That(result.Status, Is.EqualTo(LoadActiveGameStatus.RecoveryRequired));
		Assert.That(result.Session, Is.Null);
		Assert.That(result.Record, Is.Not.Null);
	}

	[Test]
	public async Task Complete_RequiresCorrectSolutionAndTransitionsExactlyOnce()
	{
		var store = new FakeGameProgressStore();
		var generated = GameSessionTestData.CreateGeneratedPuzzle();
		var created = await new CreateGameProgressUseCase(store).ExecuteAsync(generated);
		var session = created.Session!;
		var incomplete = await new CompleteGameProgressUseCase(store).ExecuteAsync(session, created.Record!);
		Assert.That(incomplete.Completion.IsCompleted, Is.False);
		Assert.That(incomplete.Persistence, Is.Null);
		Assert.That(store.Records, Has.Count.EqualTo(1));

		foreach (var cell in generated.Puzzle.CreateBoard().Cells)
			session.EnterDigit(cell.Position, GameInputMode.Answer, generated.Solution.GetValue(cell.Position));

		var completion = await new CompleteGameProgressUseCase(store).ExecuteAsync(session, created.Record!);
		var duplicateCompletion = await new CompleteGameProgressUseCase(store).ExecuteAsync(session, created.Record!);

		Assert.That(completion.Completion.IsCompleted, Is.True);
		Assert.That(completion.Persistence?.IsSaved, Is.True);
		Assert.That(duplicateCompletion.Persistence?.IsSaved, Is.True);
		Assert.That(store.Records, Has.Count.EqualTo(1));
		Assert.That(store.Records[0].Status, Is.EqualTo(GameProgressStatus.Completed));
		Assert.That(store.Active, Is.Null);
	}

	[Test]
	public async Task Abandon_StoresTerminalMetricsAndRemovesResumableSnapshot()
	{
		var store = new FakeGameProgressStore();
		var time = new ManualTimeProvider(new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero));
		var created = await new CreateGameProgressUseCase(store, time).ExecuteAsync(GameSessionTestData.CreateGeneratedPuzzle());
		created.Session!.Start();
		time.Advance(TimeSpan.FromSeconds(42));
		var position = new CellPosition(2, 0);
		created.Session.EnterDigit(position, GameInputMode.Answer, 4);
		created.Session.EnterDigit(position, GameInputMode.Answer, 5);

		var result = await new AbandonGameProgressUseCase(store, time).ExecuteAsync(created.Session, created.Record!);

		Assert.That(result.Persistence.IsSaved, Is.True);
		Assert.That(result.Record.Status, Is.EqualTo(GameProgressStatus.Abandoned));
		Assert.That(result.Record.ActiveElapsedTicks, Is.EqualTo(TimeSpan.FromSeconds(42).Ticks));
		Assert.That(result.Record.ErrorCount, Is.GreaterThan(0));
		Assert.That(store.Active, Is.Null);
		Assert.That(store.Records, Has.Count.EqualTo(1));
		Assert.That(store.Records[0].Status, Is.EqualTo(GameProgressStatus.Abandoned));
	}

	private sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
	{
		private DateTimeOffset _now = now;
		private long _timestamp;

		public override DateTimeOffset GetUtcNow() => _now;
		public override long TimestampFrequency => TimeSpan.TicksPerSecond;
		public override long GetTimestamp() => _timestamp;

		public void Advance(TimeSpan elapsed)
		{
			_now += elapsed;
			_timestamp += elapsed.Ticks;
		}
	}

	private sealed class FakeGameProgressStore : IGameProgressStore
	{
		public SavedGameSession? Active { get; set; }
		public List<GameProgressRecord> Records { get; } = [];
		public bool FailNextWrite { get; set; }
		public Action? AfterCreateCommit { get; set; }
		public Action? AfterReplaceCommit { get; set; }

		public Task<GameProgressWriteResult> CreateAsync(SavedGameSession session, CancellationToken cancellationToken = default)
		{
			if (FailNextWrite)
			{
				FailNextWrite = false;
				return Task.FromResult(GameProgressWriteResult.Failed("write-failed"));
			}
			if (Active is not null)
				return Task.FromResult(GameProgressWriteResult.Conflict("active-session"));
			Active = session;
			Records.Add(session.Record);
			AfterCreateCommit?.Invoke();
			return Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<GameProgressLoadResult> LoadActiveAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(Active is null ? GameProgressLoadResult.NoActiveSession() : GameProgressLoadResult.Loaded(Active));

		public Task<GameProgressWriteResult> SaveAsync(SavedGameSession session, CancellationToken cancellationToken = default)
		{
			if (FailNextWrite)
			{
				FailNextWrite = false;
				return Task.FromResult(GameProgressWriteResult.Failed("write-failed"));
			}
			if (Active?.SessionId != session.SessionId)
				return Task.FromResult(GameProgressWriteResult.Conflict("not-active"));
			Active = session;
			ReplaceRecord(session.Record);
			return Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<GameProgressWriteResult> CompleteAsync(GameProgressRecord completedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(Transition(completedRecord));

		public Task<GameProgressWriteResult> AbandonAsync(GameProgressRecord abandonedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(Transition(abandonedRecord));

		public Task<GameProgressWriteResult> AbandonUnrecoverableActiveAsync(string sessionKey, DateTimeOffset abandonedAtUtc, CancellationToken cancellationToken = default)
		{
			if (Active?.SessionId.ToString("D") != sessionKey)
				return Task.FromResult(GameProgressWriteResult.Conflict("not-active"));
			var recoveredRecord = Active.Record with
			{
				Status = GameProgressStatus.Abandoned,
				CompletedAtUtc = null,
				AbandonedAtUtc = abandonedAtUtc
			};
			ReplaceRecord(recoveredRecord);
			Active = null;
			return Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<GameProgressWriteResult> ReplaceActiveAsync(
			SavedGameSession replacement,
			string? replacedSessionKey,
			DateTimeOffset abandonedAtUtc,
			CancellationToken cancellationToken = default)
		{
			if (replacedSessionKey is null
				? Active is not null
				: Active?.SessionId.ToString("D") != replacedSessionKey)
				return Task.FromResult(GameProgressWriteResult.Conflict("not-active"));
			if (Active is not null)
			{
				var abandoned = Active.Record with
				{
					Status = GameProgressStatus.Abandoned,
					AbandonedAtUtc = abandonedAtUtc
				};
				ReplaceRecord(abandoned);
			}
			Active = replacement;
			ReplaceRecord(replacement.Record);
			AfterReplaceCommit?.Invoke();
			return Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<IReadOnlyList<GameProgressRecord>> GetRecordsAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<IReadOnlyList<GameProgressRecord>>(Records.AsReadOnly());

		private GameProgressWriteResult Transition(GameProgressRecord record)
		{
			if (FailNextWrite)
			{
				FailNextWrite = false;
				return GameProgressWriteResult.Failed("write-failed");
			}
			if (Active?.SessionId != record.SessionId)
				return Records.Any(existing => IsSameTerminalRecord(existing, record))
					? GameProgressWriteResult.Saved()
					: GameProgressWriteResult.Conflict("not-active");
			ReplaceRecord(record);
			Active = null;
			return GameProgressWriteResult.Saved();
		}

		private void ReplaceRecord(GameProgressRecord record)
		{
			var index = Records.FindIndex(existing => existing.SessionId == record.SessionId);
			if (index < 0)
				Records.Add(record);
			else
				Records[index] = record;
		}

		private static bool IsSameTerminalRecord(GameProgressRecord current, GameProgressRecord requested) =>
			current.SessionId == requested.SessionId &&
			current.Status == requested.Status &&
			current.StartedAtUtc == requested.StartedAtUtc &&
			current.Difficulty == requested.Difficulty &&
			current.ActiveElapsedTicks == requested.ActiveElapsedTicks &&
			current.ErrorCount == requested.ErrorCount &&
			current.DisplayedHintLevelCount == requested.DisplayedHintLevelCount &&
			(requested.Status == GameProgressStatus.Completed
				? current.CompletedAtUtc.HasValue && !current.AbandonedAtUtc.HasValue
				: current.AbandonedAtUtc.HasValue && !current.CompletedAtUtc.HasValue);
	}
}
