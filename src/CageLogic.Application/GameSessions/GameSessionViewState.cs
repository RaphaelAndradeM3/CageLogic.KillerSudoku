using System.Collections.ObjectModel;
using CageLogic.Application.Difficulty;
using CageLogic.Domain.Board;

namespace CageLogic.Application.GameSessions;

/// <summary>An immutable, presentation-ready snapshot of the active board.</summary>
public sealed class GameSessionViewState
{
	internal GameSessionViewState(
		DifficultyLevel difficulty,
		CellPosition? selectedPosition,
		bool isBoardValid,
		IEnumerable<GameSessionCellViewState> cells,
		IEnumerable<GameSessionCageViewState> cages)
	{
		Difficulty = difficulty;
		SelectedPosition = selectedPosition;
		IsBoardValid = isBoardValid;
		Cells = new ReadOnlyCollection<GameSessionCellViewState>(cells.ToArray());
		Cages = new ReadOnlyCollection<GameSessionCageViewState>(cages.ToArray());
	}

	public DifficultyLevel Difficulty { get; }

	public CellPosition? SelectedPosition { get; }

	public bool IsBoardValid { get; }

	public IReadOnlyList<GameSessionCellViewState> Cells { get; }

	public IReadOnlyList<GameSessionCageViewState> Cages { get; }
}

public sealed record GameSessionCellViewState
{
	internal GameSessionCellViewState(
		CellPosition position,
		int? value,
		bool isGiven,
		bool isSelected,
		bool hasConflict)
	{
		Position = position;
		Value = value;
		IsGiven = isGiven;
		IsEditable = !isGiven;
		IsSelected = isSelected;
		HasConflict = hasConflict;
	}

	public CellPosition Position { get; }

	public int? Value { get; }

	public bool IsGiven { get; }

	public bool IsEditable { get; }

	public bool IsSelected { get; }

	public bool HasConflict { get; }
}

public sealed record GameSessionCageViewState
{
	internal GameSessionCageViewState(int targetSum, IEnumerable<CellPosition> positions)
	{
		TargetSum = targetSum;
		Positions = new ReadOnlyCollection<CellPosition>(positions.ToArray());
	}

	public int TargetSum { get; }

	public IReadOnlyList<CellPosition> Positions { get; }
}
