using CageLogic.Application.Hints;
using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Tests.Hints;

public sealed class GetHintUseCaseTests
{
    [Test]
    public void HintPuzzleContext_RequiresSolutionOnlyForUniquePuzzle()
    {
        var uniquePuzzle = CreateUniquePuzzle();
        var solution = CreateSolution(uniquePuzzle);
        var multiplePuzzle = CreateMultiplePuzzle();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() => new HintPuzzleContext(
                uniquePuzzle,
                SolutionMultiplicity.Unique,
                uniqueSolution: null));
            Assert.Throws<ArgumentException>(() => new HintPuzzleContext(
                multiplePuzzle,
                SolutionMultiplicity.Multiple,
                solution));
            Assert.DoesNotThrow(() => new HintPuzzleContext(
                uniquePuzzle,
                SolutionMultiplicity.Unique,
                solution));
        });
    }

    [Test]
    public void HintPuzzleContext_RejectsSolutionThatDoesNotMatchPuzzleGivens()
    {
        var solutionPuzzle = CreateUniquePuzzle();
        var solution = CreateSolution(solutionPuzzle);
        var otherPuzzle = CreateMultiplePuzzle(new Dictionary<PuzzleDefinitionPosition, int>
        {
            [new PuzzleDefinitionPosition(0, 0)] = 2
        });

        Assert.Throws<ArgumentException>(() => new HintPuzzleContext(
            otherPuzzle,
            SolutionMultiplicity.Unique,
            solution));
    }

    [Test]
    public void HintRequest_RequiresBoardStructureAndGivensToMatchPuzzleContext()
    {
        var puzzle = CreateUniquePuzzle();
        var context = new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, CreateSolution(puzzle));
        var differentBoard = CreateMultiplePuzzle().CreateBoard();

        Assert.Throws<ArgumentException>(() => new HintRequest(
            context,
            differentBoard,
            HintLevel.Explanation,
            boardRevision: 1));
    }

    [Test]
    public void HintRequest_RejectsInvalidLevelAndNegativeRevision()
    {
        var puzzle = CreateUniquePuzzle();
        var context = new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, CreateSolution(puzzle));
        var board = puzzle.CreateBoard();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HintRequest(
                context,
                board,
                (HintLevel)0,
                boardRevision: 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HintRequest(
                context,
                board,
                HintLevel.Explanation,
                boardRevision: -1));
        });
    }

    [TestCaseSource(nameof(TechniqueVectors))]
    public async Task GetHintUseCase_ExplainsEachReferenceTechniqueWithoutRevealingItsAction(
        LogicalTechniqueId expectedTechniqueId,
        LogicalStep expectedStep)
    {
        var puzzle = CreateUniquePuzzle();
        var solution = CreateSolution(puzzle);
        var board = puzzle.CreateBoard();
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, solution),
            board,
            HintLevel.Explanation,
            boardRevision: 42);

        var result = await new GetHintUseCase(new FixedAnalyzer(expectedStep)).ExecuteAsync(request);
        var catalogEntry = new LogicalHintExplanationCatalog().Get(expectedTechniqueId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HintStatus.Available));
            Assert.That(result.BoardRevision, Is.EqualTo(42));
            Assert.That(result.TechniqueId, Is.EqualTo(expectedTechniqueId));
            Assert.That(result.TechniqueName, Is.EqualTo(catalogEntry.Name));
            Assert.That(result.Explanation, Is.EqualTo(catalogEntry.Explanation));
            Assert.That(expectedStep.Evidence, Is.Not.Null);
            Assert.That(expectedStep.RelatedPositions, Is.Not.Empty);
            Assert.That(expectedStep.Placement.HasValue || expectedStep.Eliminations.Count > 0, Is.True);
        });
    }

    [Test]
    public async Task GetHintUseCase_DefaultAnalyzerUsesStableTechniquePriority()
    {
        var puzzle = CreateUniquePuzzle();
        var context = new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, CreateSolution(puzzle));
        var request = new HintRequest(context, puzzle.CreateBoard(), HintLevel.Explanation, boardRevision: 3);

        var result = await new GetHintUseCase().ExecuteAsync(request);

        Assert.That(result.TechniqueId, Is.EqualTo(LogicalTechniqueId.NakedSingle));
    }

    [Test]
    public async Task GetHintUseCase_ReturnsPuzzleSolvedForCompleteCorrectBoard()
    {
        var puzzle = CreateUniquePuzzle();
        var solution = CreateSolution(puzzle);
        var board = solution.ToBoard(puzzle);
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, solution),
            board,
            HintLevel.Explanation,
            boardRevision: 5);

        var result = await new GetHintUseCase().ExecuteAsync(request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HintStatus.PuzzleSolved));
            Assert.That(result.BoardRevision, Is.EqualTo(5));
            Assert.That(result.TechniqueId, Is.Null);
            Assert.That(result.Explanation, Is.Null);
        });
    }

    [Test]
    public async Task GetHintUseCase_ReturnsInconsistentForConflictingPlayerValue()
    {
        var puzzle = CreateUniquePuzzle();
        var solution = CreateSolution(puzzle);
        var board = puzzle.CreateBoard().WithPlayerValue(new CellPosition(0, 0), 2);
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, solution),
            board,
            HintLevel.Explanation,
            boardRevision: 6);

        var result = await new GetHintUseCase().ExecuteAsync(request);

        Assert.That(result.Status, Is.EqualTo(HintStatus.InconsistentState));
    }

    [Test]
    public async Task GetHintUseCase_ReturnsInconsistentWhenOriginalPuzzleHasNoSolution()
    {
        var puzzle = CreateMultiplePuzzle();
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.NoSolution, uniqueSolution: null),
            puzzle.CreateBoard(),
            HintLevel.Explanation,
            boardRevision: 7);

        var result = await new GetHintUseCase().ExecuteAsync(request);

        Assert.That(result.Status, Is.EqualTo(HintStatus.InconsistentState));
    }

    [Test]
    public async Task GetHintUseCase_ReturnsInconsistentWhenMultipleOriginHasNoCompatibleSolution()
    {
        var puzzle = CreateMultiplePuzzle();
        var solutionValues = CreateStandardSolution(puzzle);
        var board = puzzle.CreateBoard();
        foreach (var cell in board.Cells)
        {
            if (cell.Position is { Row: 0, Column: 0 })
            {
                board = board.WithPlayerValue(cell.Position, 2);
            }
            else if (cell.Position is { Row: 0, Column: 1 } or { Row: 3, Column: 0 })
            {
                continue;
            }
            else
            {
                board = board.WithPlayerValue(cell.Position, solutionValues.GetValue(cell.Position));
            }
        }

        Assert.That(new SudokuBoardValidator().Validate(board).IsValid, Is.True);
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Multiple, uniqueSolution: null),
            board,
            HintLevel.Explanation,
            boardRevision: 8);

        var result = await new GetHintUseCase().ExecuteAsync(request);

        Assert.That(result.Status, Is.EqualTo(HintStatus.InconsistentState));
    }

    [Test]
    public async Task GetHintUseCase_ReturnsNoSafeHintWhenAnalyzerFindsNoStep()
    {
        var puzzle = CreateUniquePuzzle();
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, CreateSolution(puzzle)),
            puzzle.CreateBoard(),
            HintLevel.Explanation,
            boardRevision: 9);

        var result = await new GetHintUseCase(new FixedAnalyzer(null)).ExecuteAsync(request);

        Assert.That(result.Status, Is.EqualTo(HintStatus.NoSafeHint));
    }

    [Test]
    public async Task GetHintUseCase_PropagatesCancellation()
    {
        var puzzle = CreateUniquePuzzle();
        var request = new HintRequest(
            new HintPuzzleContext(puzzle, SolutionMultiplicity.Unique, CreateSolution(puzzle)),
            puzzle.CreateBoard(),
            HintLevel.Explanation,
            boardRevision: 10);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            new GetHintUseCase().ExecuteAsync(request, cancellation.Token));
    }

    private static IEnumerable<TestCaseData> TechniqueVectors()
    {
        var rowScope = Enumerable.Range(0, 9).Select(column => new CellPosition(0, column)).ToArray();
        var pattern = new[] { new CellPosition(0, 0), new CellPosition(0, 1) };
        var techniqueEffects = new (LogicalTechniqueId Id, LogicalStep Step)[]
        {
            (LogicalTechniqueId.NakedSingle, new LogicalStep(
                LogicalTechniqueId.NakedSingle,
                new LogicalPlacement(new CellPosition(0, 0), 1),
                evidence: new LogicalStepEvidence([new CellPosition(0, 0)], relevantDigits: [1]))),
            (LogicalTechniqueId.HiddenSingle, new LogicalStep(
                LogicalTechniqueId.HiddenSingle,
                new LogicalPlacement(new CellPosition(0, 0), 1),
                evidence: new LogicalStepEvidence([new CellPosition(0, 0)], rowScope, [1], LogicalScopeContext.ForRegion(LogicalScopeKind.Row, 0)))),
            (LogicalTechniqueId.CageSingle, new LogicalStep(
                LogicalTechniqueId.CageSingle,
                new LogicalPlacement(new CellPosition(0, 0), 1),
                evidence: new LogicalStepEvidence(pattern, pattern, [1], LogicalScopeContext.ForCage(3)))),
            (LogicalTechniqueId.CageCombination, new LogicalStep(
                LogicalTechniqueId.CageCombination,
                eliminations: [new CandidateElimination(new CellPosition(0, 2), 2)],
                evidence: new LogicalStepEvidence(pattern, pattern, [1, 2], LogicalScopeContext.ForCage(3)))),
            (LogicalTechniqueId.CageRegionIntersection, new LogicalStep(
                LogicalTechniqueId.CageRegionIntersection,
                eliminations: [new CandidateElimination(new CellPosition(0, 2), 1)],
                evidence: new LogicalStepEvidence(pattern, rowScope, [1], LogicalScopeContext.ForCageRegionIntersection(LogicalScopeKind.Row, 0, 3)))),
            (LogicalTechniqueId.RuleOf45, new LogicalStep(
                LogicalTechniqueId.RuleOf45,
                eliminations: [new CandidateElimination(new CellPosition(0, 2), 1)],
                evidence: new LogicalStepEvidence(pattern, rowScope, [1], LogicalScopeContext.ForRuleOf45(LogicalScopeKind.Row, 0, 45, [3])))),
            (LogicalTechniqueId.NakedPair, new LogicalStep(
                LogicalTechniqueId.NakedPair,
                eliminations: [new CandidateElimination(new CellPosition(0, 2), 1)],
                evidence: new LogicalStepEvidence(pattern, rowScope, [1, 2], LogicalScopeContext.ForRegion(LogicalScopeKind.Row, 0)))),
            (LogicalTechniqueId.HiddenPair, new LogicalStep(
                LogicalTechniqueId.HiddenPair,
                eliminations: [new CandidateElimination(new CellPosition(0, 0), 3)],
                evidence: new LogicalStepEvidence(pattern, rowScope, [1, 2], LogicalScopeContext.ForRegion(LogicalScopeKind.Row, 0)))),
            (LogicalTechniqueId.NakedTriple, new LogicalStep(
                LogicalTechniqueId.NakedTriple,
                eliminations: [new CandidateElimination(new CellPosition(0, 2), 3)],
                evidence: new LogicalStepEvidence(
                    [new CellPosition(0, 0), new CellPosition(0, 1), new CellPosition(0, 2)],
                    rowScope,
                    [1, 2, 3],
                    LogicalScopeContext.ForRegion(LogicalScopeKind.Row, 0))))
        };

        return techniqueEffects.Select((item, index) => new TestCaseData(item.Id, item.Step)
            .SetName($"GetHintUseCase_ExplainsLH{index + 1:00}_{item.Id}"));
    }

    private static ValidatedPuzzle CreateUniquePuzzle()
    {
        var cages = from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    let value = (row * 3 + row / 3 + column) % 9 + 1
                    select new CageDefinition(value, [new PuzzleDefinitionPosition(row, column)]);
        return Validate(new PuzzleDefinition(null, cages));
    }

    private static ValidatedPuzzle CreateMultiplePuzzle(
        IReadOnlyDictionary<PuzzleDefinitionPosition, int>? givens = null)
    {
        var cages = Enumerable.Range(0, 9)
            .Select(row => new CageDefinition(45,
                Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))));
        return Validate(new PuzzleDefinition(givens, cages));
    }

    private static ValidatedPuzzle Validate(PuzzleDefinition definition)
    {
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }

    private static SolutionGrid CreateSolution(ValidatedPuzzle puzzle)
    {
        var board = puzzle.CreateBoard();
        var values = board.Cells.Select(cell => board.GetCage(cell.Position).TargetSum);
        return new SolutionGrid(values, puzzle);
    }

    private static SolutionGrid CreateStandardSolution(ValidatedPuzzle puzzle)
    {
        var values = from row in Enumerable.Range(0, 9)
                     from column in Enumerable.Range(0, 9)
                     select (row * 3 + row / 3 + column) % 9 + 1;
        return new SolutionGrid(values, puzzle);
    }

    private sealed class FixedAnalyzer(LogicalStep? step) : ILogicalStepAnalyzer
    {
        public LogicalStep? FindNextStep(
            LogicalState state,
            IReadOnlySet<LogicalTechniqueId>? allowedTechniques = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return step;
        }
    }
}
