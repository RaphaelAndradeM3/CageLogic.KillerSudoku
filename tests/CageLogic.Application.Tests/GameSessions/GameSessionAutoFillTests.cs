using CageLogic.Application.Candidates;
using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionAutoFillTests
{
	[Test]
	public void AutoFillReplacesEveryEmptyCellNoteAsOneUndoRedoTransaction()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle(oneCagePerRow: true));
		var manualPosition = new CellPosition(0, 0);
		session.SelectCell(manualPosition);
		session.SetInputMode(GameInputMode.Candidate);
		session.EnterDigit(9);
		var expected = new GetCandidatesUseCase().Execute(session.CurrentBoardSnapshot)
			.ToDictionary(candidate => candidate.Position, candidate => candidate.Values.Order().ToArray());

		Assert.That(session.AutoFillCandidates(), Is.True);
		foreach (var (position, values) in expected)
			Assert.That(Cell(session, position).Notes, Is.EquivalentTo(values));
		Assert.That(session.CanUndo, Is.True);

		Assert.That(session.Undo(), Is.True);
		Assert.That(Cell(session, manualPosition).Notes, Is.EquivalentTo(new[] { 9 }));
		Assert.That(session.Redo(), Is.True);
		foreach (var (position, values) in expected)
			Assert.That(Cell(session, position).Notes, Is.EquivalentTo(values));
	}

	private static GameSessionCellViewState Cell(GameSession session, CellPosition position) =>
		session.ViewState.Cells.Single(cell => cell.Position == position);
}
