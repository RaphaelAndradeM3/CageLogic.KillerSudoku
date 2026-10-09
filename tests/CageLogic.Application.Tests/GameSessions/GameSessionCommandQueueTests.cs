using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionCommandQueueTests
{
	[Test]
	public async Task QueuedEdits_PreserveCapturedCellAndMode_AndApplyBothInOrder()
	{
		var generatedPuzzle = GameSessionTestData.CreateGeneratedPuzzle();
		var session = new GameSession(generatedPuzzle);
		var firstPosition = new CellPosition(0, 0);
		var secondPosition = new CellPosition(0, 1);
		var firstDigit = generatedPuzzle.Solution.GetValue(firstPosition);
		var secondDigit = generatedPuzzle.Solution.GetValue(secondPosition);
		session.SelectCell(secondPosition);
		Assert.That(session.EnterDigit(secondPosition, GameInputMode.Answer, secondDigit), Is.True);

		var queue = new GameSessionCommandQueue();
		var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirst = new ManualResetEventSlim();
		try
		{
			var first = queue.ExecuteAsync(() =>
			{
				firstStarted.SetResult();
				releaseFirst.Wait();
				return session.EnterDigit(firstPosition, GameInputMode.Answer, firstDigit);
			});
			await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

			// Selection and mode can change while validation is running; the queued clear retains its own intent.
			session.SelectCell(secondPosition);
			session.SetInputMode(GameInputMode.Answer);
			var second = queue.ExecuteAsync(() => session.ClearSelected(secondPosition, GameInputMode.Answer));

			Assert.That(second.IsCompleted, Is.False);
			releaseFirst.Set();

			Assert.That(await first.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(await second.WaitAsync(TimeSpan.FromSeconds(5)), Is.True);
			Assert.That(session.ViewState.Cells.Single(cell => cell.Position == firstPosition).Value, Is.EqualTo(firstDigit));
			Assert.That(session.ViewState.Cells.Single(cell => cell.Position == secondPosition).Value, Is.Null);
		}
		finally
		{
			releaseFirst.Set();
			releaseFirst.Dispose();
		}
	}
}
