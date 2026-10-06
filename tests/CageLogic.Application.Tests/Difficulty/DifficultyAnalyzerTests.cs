using CageLogic.Application.Difficulty;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.Difficulty;

public sealed class DifficultyAnalyzerTests
{
    [Test]
    public void Catalog_VersionOneProfilesAreCumulativeAndOrdered()
    {
        var catalog = new DifficultyProfileCatalog();
        var profiles = catalog.OrderedProfiles;

        Assert.That(catalog.Version, Is.EqualTo(1));
        Assert.That(profiles.Select(profile => profile.Level), Is.EqualTo(new[]
        {
            DifficultyLevel.Easy,
            DifficultyLevel.Medium,
            DifficultyLevel.Hard,
            DifficultyLevel.Expert
        }));
        Assert.That(profiles.Select(profile => profile.OrderedTechniques.Count), Is.EqualTo(new[] { 3, 4, 6, 9 }));
        Assert.That(profiles[0].OrderedTechniques, Is.EqualTo(new[]
        {
            LogicalTechniqueId.NakedSingle,
            LogicalTechniqueId.HiddenSingle,
            LogicalTechniqueId.CageSingle
        }));
        Assert.That(profiles[1].Techniques.IsSupersetOf(profiles[0].Techniques), Is.True);
        Assert.That(profiles[2].Techniques.IsSupersetOf(profiles[1].Techniques), Is.True);
        Assert.That(profiles[3].Techniques.IsSupersetOf(profiles[2].Techniques), Is.True);
        Assert.That(profiles[3].OrderedTechniques.Skip(6).ToArray(), Is.EqualTo(new[]
        {
            LogicalTechniqueId.NakedPair,
            LogicalTechniqueId.HiddenPair,
            LogicalTechniqueId.NakedTriple
        }));
    }

    [Test]
    public void Analyze_CompleteLogicalTrace_ClassifiesAtLowestProfile()
    {
        var result = new DifficultyAnalyzer().Analyze(CreateSolvedBySingletonCagesPuzzle());

        Assert.That(result.Status, Is.EqualTo(DifficultyAnalysisStatus.Classified));
        Assert.That(result.Level, Is.EqualTo(DifficultyLevel.Easy));
        Assert.That(result.CatalogVersion, Is.EqualTo(DifficultyProfileCatalog.CurrentVersion));
        Assert.That(result.TechniqueTrace, Has.Count.EqualTo(81));
        Assert.That(result.TechniqueTrace, Does.Contain(LogicalTechniqueId.NakedSingle));
    }

    [TestCase(DifficultyLevel.Easy, LogicalTechniqueId.NakedSingle)]
    [TestCase(DifficultyLevel.Medium, LogicalTechniqueId.CageCombination)]
    [TestCase(DifficultyLevel.Hard, LogicalTechniqueId.CageRegionIntersection)]
    [TestCase(DifficultyLevel.Expert, LogicalTechniqueId.NakedPair)]
    public void Analyze_RespectsEachTierBoundaryAndRequiresACompleteTrace(
        DifficultyLevel expectedLevel,
        LogicalTechniqueId requiredTechnique)
    {
        var scriptedAnalyzer = new ScriptedStepAnalyzer(requiredTechnique);

        var result = new DifficultyAnalyzer(
            new DifficultyProfileCatalog(),
            scriptedAnalyzer,
            new CageLogic.Domain.Validation.SudokuBoardValidator())
            .Analyze(CreateSolvedBySingletonCagesPuzzle());

        Assert.That(result.Status, Is.EqualTo(DifficultyAnalysisStatus.Classified));
        Assert.That(result.Level, Is.EqualTo(expectedLevel));
        Assert.That(result.TechniqueTrace, Has.Count.EqualTo(81));
        Assert.That(result.TechniqueTrace, Is.All.EqualTo(requiredTechnique));
    }

    [Test]
    public void Analyze_WhenNoProfileCompletes_ReturnsUnclassifiable()
    {
        var result = new DifficultyAnalyzer().Analyze(CreateContradictoryCompletePuzzle());

        Assert.That(result.Status, Is.EqualTo(DifficultyAnalysisStatus.Unclassifiable));
        Assert.That(result.Level, Is.Null);
        Assert.That(result.TechniqueTrace, Is.Empty);
    }

    [Test]
    public void FindNextStep_UsesRowMajorPositionForTechniqueTieBreak()
    {
        var puzzle = CreateOpenPuzzle();
        var board = puzzle.CreateBoard();
        var candidates = new CandidateCalculator().Calculate(board).ToDictionary(set => set.Position);
        candidates[new CellPosition(1, 0)] = new CandidateSet(new CellPosition(1, 0), [3]);
        candidates[new CellPosition(0, 8)] = new CandidateSet(new CellPosition(0, 8), [4]);

        var step = new LogicalStepAnalyzer().FindNextStep(new LogicalState(board, candidates.Values));

        Assert.That(step!.Placement, Is.EqualTo(new LogicalPlacement(new CellPosition(0, 8), 4)));
    }

    [Test]
    public void Analyze_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            () => new DifficultyAnalyzer().Analyze(CreateSolvedBySingletonCagesPuzzle(), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private static ValidatedPuzzle CreateSolvedBySingletonCagesPuzzle()
    {
        var cages = from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    let value = (row * 3 + row / 3 + column) % 9 + 1
                    select new CageDefinition(value, [new PuzzleDefinitionPosition(row, column)]);
        return Validate(cages, new Dictionary<PuzzleDefinitionPosition, int>());
    }

    private static ValidatedPuzzle CreateContradictoryCompletePuzzle()
    {
        var cages = from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    select new CageDefinition(1, [new PuzzleDefinitionPosition(row, column)]);
        var givens = (from row in Enumerable.Range(0, 9)
                      from column in Enumerable.Range(0, 9)
                      select new KeyValuePair<PuzzleDefinitionPosition, int>(new(row, column), 1))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        return Validate(cages, givens);
    }

    private static ValidatedPuzzle CreateOpenPuzzle()
    {
        return Validate(Enumerable.Range(0, 9).Select(row => new CageDefinition(45,
            Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column)))),
            new Dictionary<PuzzleDefinitionPosition, int>());
    }

    private static ValidatedPuzzle Validate(
        IEnumerable<CageDefinition> cages,
        IReadOnlyDictionary<PuzzleDefinitionPosition, int> givens)
    {
        var result = new PuzzleStructureValidator().Validate(new PuzzleDefinition(givens, cages));
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }

    private sealed class ScriptedStepAnalyzer(LogicalTechniqueId requiredTechnique) : ILogicalStepAnalyzer
    {
        public LogicalStep? FindNextStep(
            LogicalState state,
            IReadOnlySet<LogicalTechniqueId>? allowedTechniques = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (allowedTechniques is null || !allowedTechniques.Contains(requiredTechnique))
            {
                return null;
            }

            var next = state.Candidates.FirstOrDefault();
            if (next is null)
            {
                return null;
            }

            var value = (next.Position.Row * 3 + next.Position.Row / 3 + next.Position.Column) % 9 + 1;
            return new LogicalStep(requiredTechnique, new LogicalPlacement(next.Position, value));
        }
    }
}
