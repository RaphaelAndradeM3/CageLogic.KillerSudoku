using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionCompletionTests
{
	[Test]
	public void TryComplete_RejectsIncompleteAndConflictingBoards()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		Assert.That(session.TryComplete().Status, Is.EqualTo(SessionCompletionStatus.Incomplete));
		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(1);
		session.SelectCell(new CellPosition(0, 1));
		session.EnterDigit(1);
		Assert.That(session.TryComplete().Status, Is.EqualTo(SessionCompletionStatus.ConflictingBoard));
		Assert.That(session.Summary, Is.Null);
	}

	[Test]
	public void TryComplete_RejectsLocallyValidWrongSolution()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle(oneCagePerRow: true));
		EnterCompleteGrid(session, (_, _, digit) => digit == 9 ? 1 : digit + 1);

		var result = session.TryComplete();

		Assert.That(result.Status, Is.EqualTo(SessionCompletionStatus.IncorrectSolution));
		Assert.That(session.ViewState.IsBoardValid, Is.True);
		Assert.That(session.Summary, Is.Null);
	}

	[Test]
	public async Task SuccessfulCompletionSummaryIncludesHistoricalErrorAndDisplayedHintCounts()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(2);
		session.EnterDigit(1);
		Assert.That(session.ErrorCount, Is.EqualTo(1));
		Assert.That(session.Undo(), Is.True);
		Assert.That(session.Redo(), Is.True);
		Assert.That(session.TryComplete().Status, Is.EqualTo(SessionCompletionStatus.Incomplete));
		session.ClearSelected();
		Assert.That(session.ErrorCount, Is.EqualTo(1));

		for (var index = 0; index < 3; index++)
			Assert.That(await session.RequestNextHintAsync().WaitAsync(TimeSpan.FromSeconds(5)), Is.Not.Null);
		Assert.That(session.DisplayedHintLevelCount, Is.EqualTo(3));

		EnterCompleteGrid(session, (row, column, _) => GameSessionTestData.ExpectedValue(row, column));
		var result = session.TryComplete();

		Assert.That(result.Status, Is.EqualTo(SessionCompletionStatus.Completed));
		Assert.That(result.Summary, Is.Not.Null);
		Assert.That(result.Summary!.Difficulty.ToString(), Is.EqualTo("Easy"));
		Assert.That(result.Summary.ErrorCount, Is.EqualTo(1));
		Assert.That(result.Summary.DisplayedHintLevelCount, Is.EqualTo(3));
		Assert.That(session.Summary, Is.EqualTo(result.Summary));
	}

	private static void EnterCompleteGrid(GameSession session, Func<int, int, int, int> valueSelector)
	{
		foreach (var row in Enumerable.Range(0, 9))
		{
			foreach (var column in Enumerable.Range(0, 9))
			{
				session.SelectCell(new CellPosition(row, column));
				session.EnterDigit(valueSelector(row, column, GameSessionTestData.ExpectedValue(row, column)));
			}
		}
	}
}
