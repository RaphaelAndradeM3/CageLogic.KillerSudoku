namespace CageLogic.Domain.Board;

/// <summary>An immutable cell that keeps puzzle givens separate from player entries.</summary>
public sealed record SudokuCell
{
    public SudokuCell(CellPosition position, int? givenValue, int? playerValue)
    {
        ValidateDigit(givenValue, nameof(givenValue));
        ValidateDigit(playerValue, nameof(playerValue));
        if (givenValue.HasValue && playerValue.HasValue)
        {
            throw new ArgumentException("A fixed given cannot also contain a player value.", nameof(playerValue));
        }

        Position = position;
        GivenValue = givenValue;
        PlayerValue = playerValue;
    }

    public CellPosition Position { get; }

    public int? GivenValue { get; }

    public int? PlayerValue { get; }

    public int? CurrentValue => GivenValue ?? PlayerValue;

    private static void ValidateDigit(int? value, string parameterName)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "A cell value must be between 1 and 9.");
        }
    }
}
