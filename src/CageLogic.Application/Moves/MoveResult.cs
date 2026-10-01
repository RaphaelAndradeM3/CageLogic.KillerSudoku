using CageLogic.Domain.Board;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Moves;

public enum MoveRejectionReason
{
    GivenValueIsFixed
}

/// <summary>The outcome of applying a requested player move to an immutable board.</summary>
public sealed class MoveResult
{
    internal MoveResult(
        bool isApplied,
        MoveRejectionReason? rejectionReason,
        SudokuBoard board,
        BoardValidationResult validation)
    {
        IsApplied = isApplied;
        RejectionReason = rejectionReason;
        Board = board;
        Validation = validation;
    }

    public bool IsApplied { get; }

    public MoveRejectionReason? RejectionReason { get; }

    public SudokuBoard Board { get; }

    public BoardValidationResult Validation { get; }
}
