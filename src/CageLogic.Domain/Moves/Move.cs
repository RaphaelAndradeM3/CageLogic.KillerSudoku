using CageLogic.Domain.Board;

namespace CageLogic.Domain.Moves;

/// <summary>A request to set a digit or explicitly clear a player-entered value.</summary>
public sealed record Move
{
    public Move(CellPosition position, int? value)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A move value must be between 1 and 9, or null to clear.");
        }

        Position = position;
        Value = value;
    }

    public CellPosition Position { get; }

    /// <summary>Null means clear; non-null values must be digits 1 through 9.</summary>
    public int? Value { get; }

    public bool IsClear => !Value.HasValue;

    public static Move Clear(CellPosition position) => new(position, null);
}
