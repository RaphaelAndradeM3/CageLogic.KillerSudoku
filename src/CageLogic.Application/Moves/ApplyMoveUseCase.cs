using CageLogic.Domain.Board;
using CageLogic.Domain.Moves;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Moves;

/// <summary>Applies or clears a player entry and returns the new validation state.</summary>
public sealed class ApplyMoveUseCase
{
    private readonly SudokuBoardValidator _validator = new();

    public MoveResult Execute(SudokuBoard board, Move move)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(move);

        if (board.GetCell(move.Position).GivenValue.HasValue)
        {
            return new MoveResult(
                isApplied: false,
                MoveRejectionReason.GivenValueIsFixed,
                board,
                _validator.Validate(board));
        }

        var updatedBoard = board.WithPlayerValue(move.Position, move.Value);
        return new MoveResult(
            isApplied: true,
            rejectionReason: null,
            updatedBoard,
            _validator.Validate(updatedBoard));
    }
}
