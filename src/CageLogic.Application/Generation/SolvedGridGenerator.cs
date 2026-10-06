using System.Collections.ObjectModel;

namespace CageLogic.Application.Generation;

/// <summary>Creates a reproducible complete Sudoku grid through valid row, column, and digit permutations.</summary>
public sealed class SolvedGridGenerator : ISolvedGridGenerator
{
    public IReadOnlyList<int> Generate(int seed, CancellationToken cancellationToken = default)
    {
        var random = new Random(seed);
        var digits = Shuffle(Enumerable.Range(1, 9).ToArray(), random);
        var bandOrder = Shuffle(Enumerable.Range(0, 3).ToArray(), random);
        var rows = bandOrder
            .SelectMany(band => Shuffle(Enumerable.Range(0, 3).ToArray(), random).Select(offset => band * 3 + offset))
            .ToArray();
        var stackOrder = Shuffle(Enumerable.Range(0, 3).ToArray(), random);
        var columns = stackOrder
            .SelectMany(stack => Shuffle(Enumerable.Range(0, 3).ToArray(), random).Select(offset => stack * 3 + offset))
            .ToArray();
        var transpose = random.Next(2) == 1;
        var values = new int[81];

        for (var row = 0; row < 9; row++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var column = 0; column < 9; column++)
            {
                var sourceRow = transpose ? columns[column] : rows[row];
                var sourceColumn = transpose ? rows[row] : columns[column];
                var baseDigit = (sourceRow * 3 + sourceRow / 3 + sourceColumn) % 9;
                values[row * 9 + column] = digits[baseDigit];
            }
        }

        return new ReadOnlyCollection<int>(values);
    }

    private static T[] Shuffle<T>(T[] values, Random random)
    {
        var shuffled = (T[])values.Clone();
        for (var index = shuffled.Length - 1; index > 0; index--)
        {
            var otherIndex = random.Next(index + 1);
            (shuffled[index], shuffled[otherIndex]) = (shuffled[otherIndex], shuffled[index]);
        }

        return shuffled;
    }
}
