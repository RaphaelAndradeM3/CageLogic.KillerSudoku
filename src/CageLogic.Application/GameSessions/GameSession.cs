using CageLogic.Application.Generation;
using CageLogic.Application.Moves;
using CageLogic.Application.Validation;
using CageLogic.Domain.Board;
using CageLogic.Domain.Moves;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.GameSessions;

/// <summary>Coordinates player input and a read-only projection for one in-memory game.</summary>
public sealed class GameSession
{
	private readonly GeneratedPuzzle _generatedPuzzle;
	private readonly ApplyMoveUseCase _applyMove;
	private readonly ValidateBoardUseCase _validateBoard;
	private readonly HashSet<CellPosition> _conflictingPositions = [];
	private SudokuBoard _board;
	private CellPosition? _selectedPosition;
	private bool _isBoardValid;

	public GameSession(
		GeneratedPuzzle generatedPuzzle,
		ApplyMoveUseCase? applyMove = null,
		ValidateBoardUseCase? validateBoard = null)
	{
		ArgumentNullException.ThrowIfNull(generatedPuzzle);
		_generatedPuzzle = generatedPuzzle;
		_applyMove = applyMove ?? new ApplyMoveUseCase();
		_validateBoard = validateBoard ?? new ValidateBoardUseCase();
		_board = generatedPuzzle.Puzzle.CreateBoard();
		UpdateValidation(_validateBoard.Execute(_board));
	}

	public GameSessionViewState ViewState => CreateViewState();

	public void SelectCell(CellPosition position)
	{
		_selectedPosition = position;
	}

	/// <summary>Applies a digit to the selected editable cell and retains rule conflicts for correction.</summary>
	public bool EnterDigit(int digit)
	{
		if (digit is < 1 or > 9 || !_selectedPosition.HasValue)
			return false;

		var result = _applyMove.Execute(_board, new Move(_selectedPosition.Value, digit));
		if (!result.IsApplied)
			return false;

		_board = result.Board;
		UpdateValidation(_validateBoard.Execute(_board));
		return true;
	}

	public bool ClearSelected()
	{
		if (!_selectedPosition.HasValue)
			return false;

		var result = _applyMove.Execute(_board, Move.Clear(_selectedPosition.Value));
		if (!result.IsApplied)
			return false;

		_board = result.Board;
		UpdateValidation(_validateBoard.Execute(_board));
		return true;
	}

	private GameSessionViewState CreateViewState()
	{
		var cells = _board.Cells.Select(cell => new GameSessionCellViewState(
			cell.Position,
			cell.CurrentValue,
			cell.GivenValue.HasValue,
			_selectedPosition == cell.Position,
			_conflictingPositions.Contains(cell.Position)));
		var cages = _board.Cages.Select(cage => new GameSessionCageViewState(cage.TargetSum, cage.Positions));

		return new GameSessionViewState(
			_generatedPuzzle.RequestedDifficulty,
			_selectedPosition,
			_isBoardValid,
			cells,
			cages);
	}

	private void UpdateValidation(BoardValidationResult validation)
	{
		_conflictingPositions.Clear();
		foreach (var conflict in validation.Conflicts)
			_conflictingPositions.UnionWith(conflict.Positions);
		_isBoardValid = validation.IsValid;
	}
}
