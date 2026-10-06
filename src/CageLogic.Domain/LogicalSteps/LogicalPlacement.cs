using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

public readonly record struct LogicalPlacement
{
    public LogicalPlacement(CellPosition position, int value)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A placement must be a digit from 1 through 9.");
        }

        Position = position;
        Value = value;
    }

    public CellPosition Position { get; }

    public int Value { get; }
}
