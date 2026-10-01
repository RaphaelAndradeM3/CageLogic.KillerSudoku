using System.Collections.ObjectModel;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Domain.Board;

/// <summary>An immutable board created from a structurally validated puzzle.</summary>
public sealed class SudokuBoard
{
    private readonly SudokuCell[] _cells;
    private readonly ReadOnlyCollection<SudokuCell> _readOnlyCells;
    private readonly Dictionary<CellPosition, Cage> _cageByPosition;

    private SudokuBoard(IEnumerable<SudokuCell> cells, IEnumerable<Cage> cages)
    {
        _cells = cells.ToArray();
        if (_cells.Length != 81)
        {
            throw new ArgumentException("A Sudoku board must contain exactly 81 cells.", nameof(cells));
        }

        _readOnlyCells = Array.AsReadOnly(_cells);
        Cages = Array.AsReadOnly(cages.ToArray());
        _cageByPosition = Cages
            .SelectMany(cage => cage.Positions.Select(position => (position, cage)))
            .ToDictionary(pair => pair.position, pair => pair.cage);
        if (_cageByPosition.Count != 81)
        {
            throw new ArgumentException("Cages must cover every board position exactly once.", nameof(cages));
        }
    }

    public IReadOnlyList<SudokuCell> Cells => _readOnlyCells;

    public IReadOnlyList<Cage> Cages { get; }

    internal static SudokuBoard FromPuzzle(ValidatedPuzzle puzzle)
    {
        ArgumentNullException.ThrowIfNull(puzzle);

        var cells = from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    let position = new CellPosition(row, column)
                    let given = puzzle.Givens.TryGetValue(position, out var value) ? value : (int?)null
                    select new SudokuCell(position, given, playerValue: null);

        return new SudokuBoard(cells, puzzle.Cages);
    }

    public SudokuCell GetCell(CellPosition position)
    {
        return _cells[position.Row * 9 + position.Column];
    }

    public Cage GetCage(CellPosition position)
    {
        return _cageByPosition[position];
    }

    /// <summary>Returns a new board with a player value changed or cleared.</summary>
    public SudokuBoard WithPlayerValue(CellPosition position, int? value)
    {
        if (value is < 1 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A player value must be between 1 and 9.");
        }

        var currentCell = GetCell(position);
        if (currentCell.GivenValue.HasValue)
        {
            throw new InvalidOperationException("A fixed given cannot be changed by a player move.");
        }

        var changedCells = (SudokuCell[])_cells.Clone();
        changedCells[position.Row * 9 + position.Column] = new SudokuCell(position, givenValue: null, playerValue: value);
        return new SudokuBoard(changedCells, Cages);
    }
}
