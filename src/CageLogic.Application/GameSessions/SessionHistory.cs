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

	internal SessionHistoryState CaptureState() => new(
		_undo.Select(change => new SessionHistoryEntry(change.Before, change.After)).ToArray(),
		_redo.Select(change => new SessionHistoryEntry(change.Before, change.After)).ToArray());

	internal void RestoreState(SessionHistoryState state)
	{
		ArgumentNullException.ThrowIfNull(state);
		_undo.Clear();
		_redo.Clear();
		foreach (var change in state.Undo.Reverse())
			_undo.Push(new SessionChange(change.Before, change.After));
		foreach (var change in state.Redo.Reverse())
			_redo.Push(new SessionChange(change.Before, change.After));
	}

	internal static bool BoardsHaveSameValues(SudokuBoard left, SudokuBoard right)
	{
		return left.Cells.SequenceEqual(right.Cells);
	}

	private sealed record SessionChange(SessionSnapshot Before, SessionSnapshot After);
}

public sealed record SessionSnapshot(SudokuBoard Board, CandidateNotes Notes);

internal sealed record SessionHistoryEntry(SessionSnapshot Before, SessionSnapshot After);

internal sealed record SessionHistoryState(
	IReadOnlyList<SessionHistoryEntry> Undo,
	IReadOnlyList<SessionHistoryEntry> Redo);
