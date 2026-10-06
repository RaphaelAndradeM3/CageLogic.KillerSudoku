using System.Collections.ObjectModel;
using CageLogic.Domain.Board;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Solving;

/// <summary>An immutable, complete and validated solution for a particular puzzle.</summary>
public sealed class SolutionGrid
{
    private readonly ReadOnlyCollection<int> _values;

    public SolutionGrid(IEnumerable<int> values, ValidatedPuzzle puzzle)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(puzzle);

        var copy = values.ToArray();
        if (copy.Length != 81)
        {
            throw new ArgumentException("A solution grid must contain exactly 81 values.", nameof(values));
        }

        if (copy.Any(value => value is < 1 or > 9))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Solution values must be digits from 1 through 9.");
        }

        var board = CreateBoard(copy, puzzle);
        if (!new SudokuBoardValidator().Validate(board).IsSolved)
        {
            throw new ArgumentException("The values do not form a valid solution for the supplied puzzle.", nameof(values));
        }

        _values = Array.AsReadOnly(copy);
    }

    public IReadOnlyList<int> Values => _values;

    public int GetValue(CellPosition position) => _values[position.Row * 9 + position.Column];

    public SudokuBoard ToBoard(ValidatedPuzzle puzzle)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        return CreateBoard(_values, puzzle);
    }

    private static SudokuBoard CreateBoard(IReadOnlyList<int> values, ValidatedPuzzle puzzle)
    {
        var board = puzzle.CreateBoard();
        foreach (var cell in board.Cells)
        {
            var value = values[cell.Position.Row * 9 + cell.Position.Column];
            if (cell.GivenValue.HasValue)
            {
                if (cell.GivenValue.Value != value)
                {
                    throw new ArgumentException("The solution does not match a fixed given.", nameof(values));
                }

                continue;
            }

            board = board.WithPlayerValue(cell.Position, value);
        }

        return board;
    }
}
