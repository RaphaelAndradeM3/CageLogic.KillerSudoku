using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.LogicalSteps.Techniques;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Domain.Tests.LogicalSteps;

public sealed class LogicalTechniqueTests
{
    [Test]
    public void LogicalStepEvidence_NormalizesPositionsAndDigitsAndPreservesTypedScope()
    {
        var target = new CellPosition(0, 2);
        var support = new CellPosition(0, 1);
        var evidence = new LogicalStepEvidence(
            [target, support, target],
            [new CellPosition(0, 8), support],
            [5, 2, 5],
            LogicalScopeContext.ForRegion(LogicalScopeKind.Row, 0));

        Assert.Multiple(() =>
        {
            Assert.That(evidence.PatternPositions, Is.EqualTo(new[] { support, target }));
            Assert.That(evidence.ScopePositions, Is.EqualTo(new[] { support, new CellPosition(0, 8) }));
            Assert.That(evidence.RelevantDigits, Is.EqualTo(new[] { 2, 5 }));
            Assert.That(evidence.ScopeContext!.Kind, Is.EqualTo(LogicalScopeKind.Row));
            Assert.That(evidence.ScopeContext.RegionIndex, Is.EqualTo(0));
        });
    }

    [Test]
    public void LogicalStepEvidence_RejectsDigitsOutsideBoardRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LogicalStepEvidence(
            [new CellPosition(0, 0)],
            relevantDigits: [0]));
    }

    [Test]
    public void LogicalStep_CarriesEvidenceWithoutChangingRelatedPositionOrdering()
    {
        var evidence = new LogicalStepEvidence(
            [new CellPosition(0, 2)],
            [new CellPosition(0, 0), new CellPosition(0, 1)],
            [5]);
        var step = new LogicalStep(
            LogicalTechniqueId.NakedSingle,
            new LogicalPlacement(new CellPosition(0, 2), 5),
            evidence: evidence);

        Assert.That(step.Evidence, Is.SameAs(evidence));
        Assert.That(step.RelatedPositions, Is.EqualTo(new[]
        {
            new CellPosition(0, 0), new CellPosition(0, 1), new CellPosition(0, 2)
        }));
    }

    [Test]
    public void NakedSingle_PlacesOnlyCandidate()
    {
        var state = StateWithCandidates(OpenPuzzle(), (new CellPosition(0, 0), [5]));

        var step = new NakedSingleTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.NakedSingle));
        Assert.That(step.Placement, Is.EqualTo(new LogicalPlacement(new CellPosition(0, 0), 5)));
    }

    [Test]
    public void HiddenSingle_PlacesDigitWithOnePositionInRegion()
    {
        var state = StateWithCandidates(OpenPuzzle(),
            (new CellPosition(0, 0), [1, 2]),
            (new CellPosition(0, 1), [2, 3]),
            (new CellPosition(0, 2), [2, 3]),
            (new CellPosition(0, 3), [2, 3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 4), [2, 3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 5), [2, 3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 6), [2, 3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 7), [2, 3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 8), [2, 3, 4, 5, 6, 7, 8, 9]));

        var step = new HiddenSingleTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.HiddenSingle));
        Assert.That(step.Placement, Is.EqualTo(new LogicalPlacement(new CellPosition(0, 0), 1)));
    }

    [Test]
    public void CageSingle_PlacesValueSharedByEveryCageAssignment()
    {
        var puzzle = PuzzleWithCages(([(0, 0), (0, 1)], 3));
        var state = StateWithCandidates(puzzle, (new CellPosition(0, 0), [1]), (new CellPosition(0, 1), [2]));

        var step = new CageSingleTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.CageSingle));
        Assert.That(step.Placement, Is.EqualTo(new LogicalPlacement(new CellPosition(0, 0), 1)));
    }

    [Test]
    public void CageCombination_EliminatesCandidateWithoutCageSupport()
    {
        var puzzle = PuzzleWithCages(([(0, 0), (0, 1)], 3));
        var state = StateWithCandidates(puzzle, (new CellPosition(0, 0), [1, 2]), (new CellPosition(0, 1), [2]));

        var step = new CageCombinationTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.CageCombination));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 0), 2)));
    }

    [Test]
    public void CageRegionIntersection_EliminatesDigitOutsideContainingRegion()
    {
        var puzzle = PuzzleWithCages(([(0, 0), (0, 1)], 3), ([(0, 2), (1, 2)], 3));
        var state = LogicalState.Create(puzzle.CreateBoard());

        var step = new CageRegionIntersectionTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.CageRegionIntersection));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 2), 1)));
    }

    [Test]
    public void RuleOf45_UsesCrossingCageSumsToEliminateUnsupportedCandidate()
    {
        var puzzle = PuzzleWithCages(([(0, 0), (1, 0)], 5), ([(0, 1), (1, 1)], 7));
        var state = LogicalState.Create(puzzle.CreateBoard());

        var step = new RuleOf45Technique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.RuleOf45));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 0), 4)));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 1), 5)));
    }

    [Test]
    public void NakedPair_EliminatesPairDigitsFromOtherCellsInRegion()
    {
        var state = StateWithCandidates(OpenPuzzle(),
            (new CellPosition(0, 0), [1, 2]),
            (new CellPosition(0, 1), [1, 2]));

        var step = new NakedPairTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.NakedPair));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 2), 1)));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 2), 2)));
    }

    [Test]
    public void HiddenPair_RestrictsTwoCellsToSharedDigits()
    {
        var openPuzzle = OpenPuzzle();
        var state = StateWithCandidates(openPuzzle,
            (new CellPosition(0, 0), [1, 2, 3]),
            (new CellPosition(0, 1), [1, 2, 4]),
            (new CellPosition(0, 2), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 3), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 4), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 5), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 6), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 7), [3, 4, 5, 6, 7, 8, 9]),
            (new CellPosition(0, 8), [3, 4, 5, 6, 7, 8, 9]));

        var step = new HiddenPairTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.HiddenPair));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 0), 3)));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 1), 4)));
    }

    [Test]
    public void NakedTriple_EliminatesTripleDigitsFromOtherCellsInRegion()
    {
        var state = StateWithCandidates(OpenPuzzle(),
            (new CellPosition(0, 0), [1, 2]),
            (new CellPosition(0, 1), [2, 3]),
            (new CellPosition(0, 2), [1, 3]));

        var step = new NakedTripleTechnique().FindStep(state);

        Assert.That(step!.TechniqueId, Is.EqualTo(LogicalTechniqueId.NakedTriple));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 3), 1)));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 3), 2)));
        Assert.That(step.Eliminations, Does.Contain(new CandidateElimination(new CellPosition(0, 3), 3)));
    }

    [Test]
    public async Task CageAssignmentCache_AllowsConcurrentAnalysisOfTheSameState()
    {
        var state = LogicalState.Create(PuzzleWithCages(([(0, 0), (0, 1)], 3)).CreateBoard());
        var analyses = Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() => new CageSingleTechnique().FindStep(state)))
            .ToArray();

        var steps = await Task.WhenAll(analyses);

        Assert.That(steps.All(step => step?.TechniqueId == LogicalTechniqueId.CageSingle), Is.True);
    }

    private static ValidatedPuzzle OpenPuzzle()
    {
        var rows = Enumerable.Range(0, 9)
            .Select(row => new CageDefinition(45,
                Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))));
        return Validate(rows);
    }

    private static ValidatedPuzzle PuzzleWithCages(params ((int Row, int Column)[] Cells, int Sum)[] replacements)
    {
        var replaced = replacements.SelectMany(replacement => replacement.Cells
            .Select(position => (position.Row, position.Column))).ToHashSet();
        var cages = (from row in Enumerable.Range(0, 9)
                     from column in Enumerable.Range(0, 9)
                     where !replaced.Contains((row, column))
                     let target = (row * 3 + row / 3 + column) % 9 + 1
                     select new CageDefinition(target, [new PuzzleDefinitionPosition(row, column)])).ToList();
        cages.AddRange(replacements.Select(replacement => new CageDefinition(
            replacement.Sum,
            replacement.Cells.Select(position => new PuzzleDefinitionPosition(position.Row, position.Column)))));
        return Validate(cages);
    }

    private static ValidatedPuzzle Validate(IEnumerable<CageDefinition> cages)
    {
        var result = new PuzzleStructureValidator().Validate(new PuzzleDefinition(null, cages));
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }

    private static LogicalState StateWithCandidates(
        ValidatedPuzzle puzzle,
        params (CellPosition Position, int[] Values)[] overrides)
    {
        var candidates = new CandidateCalculator().Calculate(puzzle.CreateBoard()).ToDictionary(set => set.Position);
        foreach (var (position, values) in overrides)
        {
            candidates[position] = new CandidateSet(position, values);
        }

        return new LogicalState(puzzle.CreateBoard(), candidates.Values);
    }
}
