using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;
using CageLogic.Domain.Board;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionHintTests
{
	[Test]
	public async Task HintPendingState_StaysActiveAcrossStaleRestart_AndClearsAfterCurrentResult()
	{
		var firstRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var refreshedRequestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseRefreshedRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var realUseCase = new GetHintUseCase();
		var session = new GameSession(
			GameSessionTestData.CreateGeneratedPuzzle(),
			hintExecutor: async (request, _) =>
			{
				if (request.BoardRevision == 0)
				{
					firstRequestStarted.TrySetResult();
					await releaseFirstRequest.Task;
				}
				else
				{
					refreshedRequestStarted.TrySetResult();
					await releaseRefreshedRequest.Task;
				}

				return await realUseCase.ExecuteAsync(request, CancellationToken.None);
			});

		var staleTask = session.RequestNextHintAsync();
		await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(session.IsHintPending, Is.True);

		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(1);
		await refreshedRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		releaseFirstRequest.TrySetResult();

		Assert.That(await staleTask, Is.Null);
		Assert.That(session.IsHintPending, Is.True);

		releaseRefreshedRequest.TrySetResult();
		var currentHint = await session.WaitForHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(currentHint, Is.Not.Null);
		Assert.That(currentHint!.BoardRevision, Is.EqualTo(session.BoardRevision));
		Assert.That(session.IsHintPending, Is.False);
	}

	[Test]
	public async Task HintPendingState_ClearsAfterAnalysisFailure()
	{
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var session = new GameSession(
			GameSessionTestData.CreateGeneratedPuzzle(),
			hintExecutor: async (_, _) =>
			{
				started.TrySetResult();
				await release.Task;
				throw new InvalidOperationException("Simulated analysis failure.");
			});

		var hintTask = session.RequestNextHintAsync();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(session.IsHintPending, Is.True);
		release.TrySetResult();

		await Assert.ThrowsAsync<InvalidOperationException>(async () => await hintTask);
		Assert.That(session.IsHintPending, Is.False);
	}

	[Test]
	public async Task StaleResultIsDiscarded_AndBoardEditRestartsAtExplanation()
	{
		var firstRequestStarted = new TaskCompletionSource<HintRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirstRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var requests = new List<HintRequest>();
		var realUseCase = new GetHintUseCase();
		var session = new GameSession(
			GameSessionTestData.CreateGeneratedPuzzle(),
			hintExecutor: async (request, _) =>
			{
				lock (requests)
					requests.Add(request);
				if (request.BoardRevision == 0)
				{
					firstRequestStarted.TrySetResult(request);
					await releaseFirstRequest.Task;
				}
				return await realUseCase.ExecuteAsync(request, CancellationToken.None);
			});

		var staleTask = session.RequestNextHintAsync();
		var originalRequest = await firstRequestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(originalRequest.Level, Is.EqualTo(HintLevel.Explanation));
		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(1);
		Assert.That(session.BoardRevision, Is.EqualTo(1));
		releaseFirstRequest.TrySetResult();

		Assert.That(await staleTask, Is.Null);
		var refreshed = await session.WaitForHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(refreshed, Is.Not.Null);
		Assert.That(refreshed!.BoardRevision, Is.EqualTo(session.BoardRevision));
		Assert.That(refreshed.Status, Is.EqualTo(HintStatus.Available));
		Assert.That(refreshed.Level, Is.EqualTo(HintLevel.Explanation));
		lock (requests)
			Assert.That(requests.Any(request => request.BoardRevision == 1 && request.Level == HintLevel.Explanation), Is.True);
	}

	[Test]
	public async Task CandidateOnlyEditDoesNotChangeRevisionOrInvalidatePendingHint()
	{
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var realUseCase = new GetHintUseCase();
		var session = new GameSession(
			GameSessionTestData.CreateGeneratedPuzzle(),
			hintExecutor: async (request, _) =>
			{
				started.TrySetResult();
				await release.Task;
				return await realUseCase.ExecuteAsync(request, CancellationToken.None);
			});

		var hintTask = session.RequestNextHintAsync();
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		session.SetInputMode(GameInputMode.Candidate);
		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(9);
		Assert.That(session.BoardRevision, Is.EqualTo(0));
		release.TrySetResult();
		var hint = await hintTask.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.That(hint, Is.Not.Null);
		Assert.That(session.DisplayedHintLevelCount, Is.EqualTo(1));
	}

	[Test]
	public async Task CancellationRestartsProgression_AndNoSafeHintDoesNotCount()
	{
		var rowCagePuzzle = GameSessionTestData.CreateGeneratedPuzzle(oneCagePerRow: true);
		var noSafeSession = new GameSession(rowCagePuzzle, getHintUseCase: new GetHintUseCase(new NoStepAnalyzer()));
		var noSafe = await noSafeSession.RequestNextHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(noSafe?.Status, Is.EqualTo(HintStatus.NoSafeHint));
		Assert.That(noSafeSession.DisplayedHintLevelCount, Is.Zero);

		var realUseCase = new GetHintUseCase();
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var gatedSession = new GameSession(
			GameSessionTestData.CreateGeneratedPuzzle(),
			hintExecutor: async (request, _) =>
			{
				started.TrySetResult();
				await release.Task;
				return await realUseCase.ExecuteAsync(request, CancellationToken.None);
			});
		using var cancellation = new CancellationTokenSource();
		var cancelledTask = gatedSession.RequestNextHintAsync(cancellation.Token);
		await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
		cancellation.Cancel();
		release.TrySetResult();
		Assert.That(await cancelledTask, Is.Null);
		Assert.That(gatedSession.IsHintPending, Is.False);
		var afterCancellation = await gatedSession.RequestNextHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(afterCancellation?.Level, Is.EqualTo(HintLevel.Explanation));
		Assert.That(gatedSession.DisplayedHintLevelCount, Is.EqualTo(1));
	}

	private sealed class NoStepAnalyzer : ILogicalStepAnalyzer
	{
		public LogicalStep? FindNextStep(
			LogicalState state,
			IReadOnlySet<LogicalTechniqueId>? allowedTechniques = null,
			CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return null;
		}
	}
}
