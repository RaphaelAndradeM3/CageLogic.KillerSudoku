using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.Solving;

internal static class SolverPuzzleFixtures
{
    public static ValidatedPuzzle CreateUniquePuzzle(
        IReadOnlyDictionary<PuzzleDefinitionPosition, int>? givens = null)
    {
        var definition = new PuzzleDefinition(givens, CreateSingletonCages());
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }

    public static ValidatedPuzzle CreateAmbiguousPuzzle()
    {
        var rows = Enumerable.Range(0, 9)
            .Select(row => new CageDefinition(45,
                Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))))
            .ToArray();
        var result = new PuzzleStructureValidator().Validate(new PuzzleDefinition(null, rows));
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }

    public static IReadOnlyDictionary<PuzzleDefinitionPosition, int> Givens(
        params (int Row, int Column, int Value)[] entries)
    {
        return entries.ToDictionary(
            entry => new PuzzleDefinitionPosition(entry.Row, entry.Column),
            entry => entry.Value);
    }

    private static CageDefinition[] CreateSingletonCages()
    {
        return (from row in Enumerable.Range(0, 9)
                from column in Enumerable.Range(0, 9)
                let value = (row * 3 + row / 3 + column) % 9 + 1
                select new CageDefinition(value, [new PuzzleDefinitionPosition(row, column)]))
            .ToArray();
    }
}
