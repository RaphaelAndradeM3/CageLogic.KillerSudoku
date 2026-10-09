using CageLogic.Domain.Board;

namespace CageLogic.Application.GameSessions;

/// <summary>Undo/redo stacks for complete board-and-note snapshots.</summary>
public sealed class SessionHistory
{
	private readonly Stack<SessionChange> _undo = new();
	private readonly Stack<SessionChange> _redo = new();

	public bool CanUndo => _undo.Count > 0;

	public bool CanRedo => _redo.Count > 0;

	public bool Record(SudokuBoard beforeBoard, CandidateNotes beforeNotes, SudokuBoard afterBoard, CandidateNotes afterNotes)
	{
		ArgumentNullException.ThrowIfNull(beforeBoard);
		ArgumentNullException.ThrowIfNull(beforeNotes);
		ArgumentNullException.ThrowIfNull(afterBoard);
		ArgumentNullException.ThrowIfNull(afterNotes);
		if (BoardsHaveSameValues(beforeBoard, afterBoard) && beforeNotes.ContentEquals(afterNotes))
			return false;

		_undo.Push(new SessionChange(new SessionSnapshot(beforeBoard, beforeNotes), new SessionSnapshot(afterBoard, afterNotes)));
		_redo.Clear();
		return true;
	}

	public SessionSnapshot? Undo()
	{
		if (!_undo.TryPop(out var change))
			return null;
		_redo.Push(change);
		return change.Before;
	}

	public SessionSnapshot? Redo()
	{
		if (!_redo.TryPop(out var change))
			return null;
		_undo.Push(change);
		return change.After;
	}

	internal static bool BoardsHaveSameValues(SudokuBoard left, SudokuBoard right)
	{
		return left.Cells.SequenceEqual(right.Cells);
	}

	private sealed record SessionChange(SessionSnapshot Before, SessionSnapshot After);
}

public sealed record SessionSnapshot(SudokuBoard Board, CandidateNotes Notes);
