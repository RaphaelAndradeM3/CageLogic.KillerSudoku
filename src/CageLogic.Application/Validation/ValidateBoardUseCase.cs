using CageLogic.Domain.Board;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Validation;

/// <summary>Validates the current values of an existing board without changing it.</summary>
public sealed class ValidateBoardUseCase
{
    private readonly SudokuBoardValidator _validator = new();

    public BoardValidationResult Execute(SudokuBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return _validator.Validate(board);
    }
}
