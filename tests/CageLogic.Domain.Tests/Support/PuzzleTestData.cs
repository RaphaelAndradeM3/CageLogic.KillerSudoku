using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Domain.Tests.Support;

internal static class PuzzleTestData
{
    public static CageDefinition[] CreateSingletonCages()
    {
        return (from row in Enumerable.Range(0, 9)
                from column in Enumerable.Range(0, 9)
                let target = (row * 3 + row / 3 + column) % 9 + 1
                select new CageDefinition(target, [new PuzzleDefinitionPosition(row, column)]))
            .ToArray();
    }

    public static PuzzleDefinition CreateDefinition(
        IEnumerable<CageDefinition>? cages = null,
        IReadOnlyDictionary<PuzzleDefinitionPosition, int>? givens = null)
    {
        return new PuzzleDefinition(givens, cages ?? CreateSingletonCages());
    }

    public static PuzzleDefinition WithCage(
        IEnumerable<(int Row, int Column)> positions,
        int targetSum,
        IReadOnlyDictionary<PuzzleDefinitionPosition, int>? givens = null)
    {
        var cagePositions = positions.ToArray();
        var replaced = cagePositions.ToHashSet();
        var cages = CreateSingletonCages()
            .Where(cage => !replaced.Contains((cage.Positions[0].Row, cage.Positions[0].Column)))
            .ToList();
        cages.Add(new CageDefinition(
            targetSum,
            cagePositions.Select(position => new PuzzleDefinitionPosition(position.Row, position.Column))));

        return CreateDefinition(cages, givens);
    }

    public static IReadOnlyDictionary<PuzzleDefinitionPosition, int> Givens(
        params (int Row, int Column, int Value)[] entries)
    {
        return entries.ToDictionary(
            entry => new PuzzleDefinitionPosition(entry.Row, entry.Column),
            entry => entry.Value);
    }
}
