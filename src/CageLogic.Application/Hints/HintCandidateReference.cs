using CageLogic.Domain.Board;

namespace CageLogic.Application.Hints;

/// <summary>A candidate digit involved at a board position, without implying that it should be removed.</summary>
public readonly record struct HintCandidateReference
{
    public HintCandidateReference(CellPosition position, int value)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A candidate digit must be between 1 and 9.");
        }

        Position = position;
        Value = value;
    }

    public CellPosition Position { get; }

    public int Value { get; }
}
