using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

/// <summary>A candidate position and digit that participates in a logical pattern.</summary>
public readonly record struct LogicalPatternCandidate
{
    public LogicalPatternCandidate(CellPosition position, int value)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A pattern candidate must be a digit from 1 through 9.");
        }

        Position = position;
        Value = value;
    }

    public CellPosition Position { get; }

    public int Value { get; }
}
