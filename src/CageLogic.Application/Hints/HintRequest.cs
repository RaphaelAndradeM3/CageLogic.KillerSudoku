using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Application.Hints;

/// <summary>Immutable request bound to the board revision analyzed by the session.</summary>
public sealed class HintRequest
{
    public HintRequest(
        HintPuzzleContext puzzleContext,
        SudokuBoard currentBoard,
        HintLevel level,
        long boardRevision)
    {
        ArgumentNullException.ThrowIfNull(puzzleContext);
        ArgumentNullException.ThrowIfNull(currentBoard);
        if (!Enum.IsDefined(level))
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (boardRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boardRevision));
        }

        if (!MatchesPuzzle(currentBoard, puzzleContext))
        {
            throw new ArgumentException("The board cages and fixed givens must match the original puzzle.", nameof(currentBoard));
        }

        PuzzleContext = puzzleContext;
        CurrentBoard = currentBoard;
        Level = level;
        BoardRevision = boardRevision;
    }

    public HintPuzzleContext PuzzleContext { get; }

    public SudokuBoard CurrentBoard { get; }

    public HintLevel Level { get; }

    public long BoardRevision { get; }

    private static bool MatchesPuzzle(SudokuBoard board, HintPuzzleContext context)
    {
        foreach (var cell in board.Cells)
        {
            var expectedGiven = context.Puzzle.Givens.TryGetValue(cell.Position, out var value) ? value : (int?)null;
            if (cell.GivenValue != expectedGiven)
            {
                return false;
            }
        }

        var expectedCages = context.Puzzle.Cages.Select(CageKey).Order(StringComparer.Ordinal);
        var actualCages = board.Cages.Select(CageKey).Order(StringComparer.Ordinal);
        return expectedCages.SequenceEqual(actualCages, StringComparer.Ordinal);
    }

    private static string CageKey(Cage cage)
    {
        var positions = cage.Positions
            .Select(position => position.Row * 9 + position.Column)
            .Order();
        return $"{cage.TargetSum}:{string.Join(',', positions)}";
    }
}
