using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Generation;

/// <summary>Applies a seed-derived Sudoku-preserving board rotation or reflection.</summary>
internal static class ExpertPuzzleSymmetry
{
    public static int[] CreateIndexMap(int seed)
    {
        var symmetry = (int)(unchecked((uint)(seed - 2)) % 8);
        var indexes = new int[81];
        for (var source = 0; source < indexes.Length; source++)
        {
            var row = source / 9;
            var column = source % 9;
            var mapped = symmetry switch
            {
                0 => (row, column),
                1 => (column, 8 - row),
                2 => (8 - row, 8 - column),
                3 => (8 - column, row),
                4 => (8 - row, column),
                5 => (row, 8 - column),
                6 => (column, row),
                _ => (8 - column, 8 - row)
            };
            indexes[source] = mapped.Item1 * 9 + mapped.Item2;
        }

        return indexes;
    }

    public static IReadOnlyList<int> TransformSolution(IReadOnlyList<int> solution, int seed)
    {
        ArgumentNullException.ThrowIfNull(solution);
        if (solution.Count != 81)
        {
            throw new ArgumentException("The solved grid must contain 81 digits.", nameof(solution));
        }

        var indexMap = CreateIndexMap(seed);
        var transformed = new int[81];
        for (var source = 0; source < indexMap.Length; source++)
        {
            transformed[indexMap[source]] = solution[source];
        }

        return transformed;
    }

    public static int[] InverseTransformSolution(IReadOnlyList<int> solution, IReadOnlyList<int> indexMap)
    {
        var canonical = new int[81];
        for (var source = 0; source < indexMap.Count; source++)
        {
            canonical[source] = solution[indexMap[source]];
        }

        return canonical;
    }

    public static IReadOnlyList<CageDefinition> TransformCages(
        IEnumerable<CageDefinition> cages,
        IReadOnlyList<int> indexMap)
    {
        return cages.Select(cage => new CageDefinition(
                cage.TargetSum,
                cage.Positions
                    .Select(position => indexMap[position.Row * 9 + position.Column])
                    .Order()
                    .Select(index => new PuzzleDefinitionPosition(index / 9, index % 9))))
            .ToArray();
    }

}
