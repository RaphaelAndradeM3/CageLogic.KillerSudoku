using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionContentHistoryTests
{
	[Test]
	public void AnswerEntryClearsNotes_UndoRestoresThem_ButLaterDeletionDoesNot()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		var position = new CellPosition(0, 0);
		session.SelectCell(position);
		session.SetInputMode(GameInputMode.Candidate);
		session.EnterDigit(8);
		Assert.That(Cell(session, position).Notes, Is.EquivalentTo(new[] { 8 }));

		session.SetInputMode(GameInputMode.Answer);
		session.EnterDigit(GameSessionTestData.ExpectedValue(0, 0));
		Assert.That(Cell(session, position).Notes, Is.Empty);
		Assert.That(session.Undo(), Is.True);
		Assert.That(Cell(session, position).Value, Is.Null);
		Assert.That(Cell(session, position).Notes, Is.EquivalentTo(new[] { 8 }));

		session.EnterDigit(GameSessionTestData.ExpectedValue(0, 0));
		session.ClearSelected();
		Assert.That(Cell(session, position).Value, Is.Null);
		Assert.That(Cell(session, position).Notes, Is.Empty);
	}

	[Test]
	public void ConflictingEntryCountsOnce_AndCorrectionUndoRedoDoNotChangeHistoricalCount()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		session.SelectCell(new CellPosition(0, 0));
		session.EnterDigit(1);
		session.SelectCell(new CellPosition(0, 1));
		session.EnterDigit(1);

		Assert.That(session.ErrorCount, Is.EqualTo(1));
		session.EnterDigit(GameSessionTestData.ExpectedValue(0, 1));
		Assert.That(session.ViewState.IsBoardValid, Is.True);
		Assert.That(session.ErrorCount, Is.EqualTo(1));

		Assert.That(session.Undo(), Is.True);
		Assert.That(session.ViewState.IsBoardValid, Is.False);
		Assert.That(session.ErrorCount, Is.EqualTo(1));
		Assert.That(session.Redo(), Is.True);
		Assert.That(session.ViewState.IsBoardValid, Is.True);
		Assert.That(session.ErrorCount, Is.EqualTo(1));
	}

	[Test]
	public void NewEditClearsRedo_AndRestoredValueChangesAdvanceRevisionMonotonically()
	{
		var session = new GameSession(GameSessionTestData.CreateGeneratedPuzzle());
		var position = new CellPosition(0, 0);
		session.SelectCell(position);
		session.EnterDigit(GameSessionTestData.ExpectedValue(0, 0));
		Assert.That(session.BoardRevision, Is.EqualTo(1));
		session.SetInputMode(GameInputMode.Candidate);
		session.Pause();
		Assert.That(session.Undo(), Is.True);
		Assert.That(session.BoardRevision, Is.EqualTo(2));
		Assert.That(session.CanRedo, Is.True);
		Assert.That(session.InputMode, Is.EqualTo(GameInputMode.Candidate));
		Assert.That(session.IsPaused, Is.True);
		Assert.That(session.ViewState.SelectedPosition, Is.EqualTo(position));
		Assert.That(session.Redo(), Is.True);
		Assert.That(session.BoardRevision, Is.EqualTo(3));

		Assert.That(session.Undo(), Is.True);
		session.EnterDigit(2);
		Assert.That(session.CanRedo, Is.False);
		Assert.That(session.BoardRevision, Is.EqualTo(4));
	}

	private static GameSessionCellViewState Cell(GameSession session, CellPosition position) =>
		session.ViewState.Cells.Single(cell => cell.Position == position);
}
