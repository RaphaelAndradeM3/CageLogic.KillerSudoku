using CageLogic.Application.GameSessions;
using CageLogic.Application.Progression;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameProgressCommandQueueTests
{
	[Test]
	public async Task Edit_IsNotConfirmedUntilItsSnapshotCommitCompletes()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		var store = new CapturingProgressStore();
		var saveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseSave = new TaskCompletionSource<GameProgressWriteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		store.SaveHandler = (_, _) =>
		{
			saveStarted.TrySetResult();
			return releaseSave.Task;
		};
		var queue = CreateQueue(session, store);
		var position = new CellPosition(0, 0);
		var command = queue.ExecuteAsync(() => session.EnterDigit(position, GameInputMode.Answer, 1));

		await saveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(command.IsCompleted, Is.False);
		Assert.That(session.CurrentBoardSnapshot.GetCell(position).CurrentValue, Is.EqualTo(1));

		releaseSave.SetResult(GameProgressWriteResult.Saved());
		var result = await command.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(result.IsConfirmed, Is.True);
		Assert.That(result.Applied, Is.True);
	}

	[Test]
	public async Task QueuedEdits_ArePersistedInMutationOrder()
	{
		var generated = GameSessionTestData.CreateGeneratedPuzzle();
		var session = new GameSession(generated);
		var store = new CapturingProgressStore();
		var queue = CreateQueue(session, store);
		var firstPosition = new CellPosition(0, 0);
		var secondPosition = new CellPosition(0, 1);
		var first = queue.ExecuteAsync(() => session.EnterDigit(firstPosition, GameInputMode.Answer, generated.Solution.GetValue(firstPosition)));
		var second = queue.ExecuteAsync(() => session.EnterDigit(secondPosition, GameInputMode.Answer, generated.Solution.GetValue(secondPosition)));

		Assert.That((await first.WaitAsync(TimeSpan.FromSeconds(5))).IsConfirmed, Is.True);
		Assert.That((await second.WaitAsync(TimeSpan.FromSeconds(5))).IsConfirmed, Is.True);
		Assert.That(store.Saves, Has.Count.EqualTo(2));
		var mapper = new GameSessionPersistenceMapper();
		Assert.That(mapper.Deserialize(store.Saves[0].SnapshotJson).CurrentValues[0], Is.EqualTo(generated.Solution.GetValue(firstPosition)));
		Assert.That(mapper.Deserialize(store.Saves[0].SnapshotJson).CurrentValues[1], Is.Null);
		Assert.That(mapper.Deserialize(store.Saves[1].SnapshotJson).CurrentValues[1], Is.EqualTo(generated.Solution.GetValue(secondPosition)));
	}

	[Test]
	public async Task SaveFailure_LeavesMutationInMemoryAndReturnsUnconfirmedResult()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		var store = new CapturingProgressStore
		{
			SaveHandler = (_, _) => Task.FromResult(GameProgressWriteResult.Failed("disk-full"))
		};
		var queue = CreateQueue(session, store);
		var position = new CellPosition(1, 2);

		var result = await queue.ExecuteAsync(() => session.EnterDigit(position, GameInputMode.Answer, 5));

		Assert.That(result.Applied, Is.True);
		Assert.That(result.IsConfirmed, Is.False);
		Assert.That(result.Persistence?.Status, Is.EqualTo(GameProgressWriteStatus.Failed));
		Assert.That(session.CurrentBoardSnapshot.GetCell(position).CurrentValue, Is.EqualTo(5));
	}

	[Test]
	public async Task FailedInitialCreate_IsRetriedByTheNextPersistedEdit()
	{
		var generated = GameSessionTestData.CreateGeneratedPuzzle();
		var session = new GameSession(generated);
		var store = new CapturingProgressStore
		{
			CreateHandler = (_, call) => Task.FromResult(call == 1
				? GameProgressWriteResult.Failed("disk-full")
				: GameProgressWriteResult.Saved())
		};
		var record = new GameProgressRecord(
			Guid.NewGuid(), GameProgressStatus.Active, DateTimeOffset.UtcNow, null, null,
			session.ViewState.Difficulty, 0, 0, 0);
		var queue = new GameProgressCommandQueue(session, record, store, wasPersisted: false);

		var first = await queue.ExecuteAsync(() => session.EnterDigit(new CellPosition(1, 2), GameInputMode.Answer, 5));
		var second = await queue.ExecuteAsync(() => session.EnterDigit(new CellPosition(1, 3), GameInputMode.Answer, 6));

		Assert.That(first.IsConfirmed, Is.False);
		Assert.That(second.IsConfirmed, Is.True);
		Assert.That(store.CreateCalls, Is.EqualTo(2));
		Assert.That(queue.IsPersisted, Is.True);
		Assert.That(store.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task CancellationBeforeQueuedEdit_RunsNoMutationAndDoesNotCancelPriorCommit()
	{
		var generated = GameSessionTestData.CreateGeneratedPuzzle();
		var session = new GameSession(generated);
		var store = new CapturingProgressStore();
		var firstSaveStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirstSave = new TaskCompletionSource<GameProgressWriteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
		store.SaveHandler = (_, call) =>
		{
			if (call == 1)
			{
				firstSaveStarted.TrySetResult();
				return releaseFirstSave.Task;
			}
			return Task.FromResult(GameProgressWriteResult.Saved());
		};
		var queue = CreateQueue(session, store);
		var firstPosition = new CellPosition(0, 0);
		var secondPosition = new CellPosition(0, 1);
		var first = queue.ExecuteAsync(() => session.EnterDigit(firstPosition, GameInputMode.Answer, generated.Solution.GetValue(firstPosition)));
		await firstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		using var cancellation = new CancellationTokenSource();
		var second = queue.ExecuteAsync(() => session.EnterDigit(secondPosition, GameInputMode.Answer, generated.Solution.GetValue(secondPosition)), cancellation.Token);
		cancellation.Cancel();
		releaseFirstSave.SetResult(GameProgressWriteResult.Saved());

		Assert.That((await first.WaitAsync(TimeSpan.FromSeconds(5))).IsConfirmed, Is.True);
		await Assert.ThrowsAsync<OperationCanceledException>(async () => await second);
		Assert.That(session.CurrentBoardSnapshot.GetCell(firstPosition).CurrentValue, Is.EqualTo(generated.Solution.GetValue(firstPosition)));
		Assert.That(session.CurrentBoardSnapshot.GetCell(secondPosition).CurrentValue, Is.Null);
		Assert.That(store.Saves, Has.Count.EqualTo(1));
	}

	[Test]
	public async Task PersistCurrent_StoresCounterForAnActuallyDisplayedHint()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		var store = new CapturingProgressStore();
		var queue = CreateQueue(session, store);

		var hint = await session.RequestNextHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(hint?.Status, Is.EqualTo(CageLogic.Application.Hints.HintStatus.Available));
		Assert.That(session.DisplayedHintLevelCount, Is.EqualTo(1));

		var result = await queue.PersistCurrentAsync();

		Assert.That(result.IsConfirmed, Is.True);
		Assert.That(store.Saves, Has.Count.EqualTo(1));
		Assert.That(new GameSessionPersistenceMapper().Deserialize(store.Saves[0].SnapshotJson).DisplayedHintLevelCount, Is.EqualTo(1));
	}

	private static GameProgressCommandQueue CreateQueue(GameSession session, CapturingProgressStore store)
	{
		var record = new GameProgressRecord(
			Guid.NewGuid(),
			GameProgressStatus.Active,
			new DateTimeOffset(2026, 10, 9, 12, 0, 0, TimeSpan.Zero),
			null,
			null,
			session.ViewState.Difficulty,
			0,
			0,
			0);
		return new GameProgressCommandQueue(session, record, store);
	}

	private sealed class CapturingProgressStore : IGameProgressStore
	{
		private readonly object _sync = new();
		private int _saveCalls;

		public Func<SavedGameSession, int, Task<GameProgressWriteResult>>? SaveHandler { get; set; }
		public Func<SavedGameSession, int, Task<GameProgressWriteResult>>? CreateHandler { get; set; }

		public List<SavedGameSession> Saves { get; } = [];
		public int CreateCalls { get; private set; }
		public int SaveCalls => _saveCalls;

		public Task<GameProgressWriteResult> CreateAsync(SavedGameSession session, CancellationToken cancellationToken = default)
		{
			CreateCalls++;
			return CreateHandler?.Invoke(session, CreateCalls) ?? Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<GameProgressLoadResult> LoadActiveAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressLoadResult.NoActiveSession());

		public Task<GameProgressWriteResult> SaveAsync(SavedGameSession session, CancellationToken cancellationToken = default)
		{
			int call;
			lock (_sync)
			{
				Saves.Add(session);
				call = ++_saveCalls;
			}
			return SaveHandler?.Invoke(session, call) ?? Task.FromResult(GameProgressWriteResult.Saved());
		}

		public Task<GameProgressWriteResult> CompleteAsync(GameProgressRecord completedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressWriteResult> AbandonAsync(GameProgressRecord abandonedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressWriteResult> AbandonUnrecoverableActiveAsync(Guid sessionId, DateTimeOffset abandonedAtUtc, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<IReadOnlyList<GameProgressRecord>> GetRecordsAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<IReadOnlyList<GameProgressRecord>>(Array.Empty<GameProgressRecord>());
	}
}
