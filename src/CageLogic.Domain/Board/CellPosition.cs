namespace CageLogic.Domain.Board;

/// <summary>A coordinate on a 9 by 9 Sudoku board, using zero-based row and column indices.</summary>
public readonly record struct CellPosition
{
    public CellPosition(int row, int column)
    {
        if (row is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(row), row, "Row must be between 0 and 8.");
        }

        if (column is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(column), column, "Column must be between 0 and 8.");
        }

        Row = row;
        Column = column;
    }

    public int Row { get; }

    public int Column { get; }
}
