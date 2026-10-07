using CageLogic.Application.Hints;
using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Tests.Hints;

public sealed class LogicalHintSnapshotAcceptanceTests
{
    [TestCaseSource(nameof(AcceptanceVectors))]
    public async Task ReferenceVector_UsesTheRealAnalyzerAndProjectsExactEffects(HintAcceptanceVector vector)
    {
        var puzzle = CreatePuzzle(vector.Givens);
        var solutionSearch = new SudokuSolver().FindSolutions(puzzle);
        Assert.That(solutionSearch.Multiplicity, Is.Not.EqualTo(SolutionMultiplicity.NoSolution), vector.Id);
        if (vector.ExpectedValue.HasValue)
        {
            Assert.That(solutionSearch.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique), vector.Id);
        }

        var uniqueSolution = solutionSearch.Multiplicity == SolutionMultiplicity.Unique
            ? solutionSearch.FirstSolution
            : null;
        var context = new HintPuzzleContext(puzzle, solutionSearch.Multiplicity, uniqueSolution);
        var board = puzzle.CreateBoard();
        var state = LogicalState.Create(board);
        var step = new LogicalStepAnalyzer().FindNextStep(state);
        var expectedPattern = ToPositions(vector.PatternIndexes);
        var expectedScope = ToPositions(vector.ScopeIndexes);
        var expectedEliminations = vector.Eliminations;
        var expectedAffected = expectedEliminations
            .Select(elimination => elimination.Position)
            .Distinct()
            .OrderBy(position => position.Row * 9 + position.Column)
            .ToArray();
        var expectedCandidates = expectedEliminations
            .Select(elimination => new HintCandidateReference(elimination.Position, elimination.Value))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(step, Is.Not.Null, vector.Id);
            Assert.That(step!.TechniqueId, Is.EqualTo(vector.TechniqueId), vector.Id);
            Assert.That(step.Evidence!.PatternPositions, Is.EqualTo(expectedPattern), vector.Id);
            Assert.That(step.Evidence.ScopePositions, Is.EqualTo(expectedScope), vector.Id);
            Assert.That(step.Evidence.RelevantDigits, Is.EqualTo(vector.RelevantDigits), vector.Id);
            Assert.That(step.Eliminations, Is.EqualTo(expectedEliminations), vector.Id);
            Assert.That(step.Placement?.Position, Is.EqualTo(vector.ExpectedPosition), vector.Id);
            Assert.That(step.Placement?.Value, Is.EqualTo(vector.ExpectedValue), vector.Id);
        });

        var useCase = new GetHintUseCase();
        var explanation = await useCase.ExecuteAsync(new HintRequest(context, board, HintLevel.Explanation, 100));
        var highlights = await useCase.ExecuteAsync(new HintRequest(context, board, HintLevel.Highlights, 100));
        var action = await useCase.ExecuteAsync(new HintRequest(context, board, HintLevel.Action, 100));
        var catalogEntry = new LogicalHintExplanationCatalog().Get(vector.TechniqueId);
        var expectedRoles = new List<HintHighlightRole> { HintHighlightRole.Pattern };
        if (expectedScope.Length > 0)
        {
            expectedRoles.Add(HintHighlightRole.Scope);
        }

        if (vector.ExpectedPosition.HasValue)
        {
            expectedRoles.Add(HintHighlightRole.Target);
        }

        if (expectedAffected.Length > 0)
        {
            expectedRoles.Add(HintHighlightRole.Affected);
        }

        Assert.Multiple(() =>
        {
            Assert.That(explanation.Status, Is.EqualTo(HintStatus.Available), vector.Id);
            Assert.That(explanation.BoardRevision, Is.EqualTo(100), vector.Id);
            Assert.That(explanation.TechniqueId, Is.EqualTo(vector.TechniqueId), vector.Id);
            Assert.That(explanation.TechniqueName, Is.EqualTo(catalogEntry.Name), vector.Id);
            Assert.That(explanation.Explanation, Is.EqualTo(catalogEntry.Explanation), vector.Id);
            Assert.That(explanation.Highlights, Is.Empty, vector.Id);
            Assert.That(explanation.InvolvedCandidates, Is.Empty, vector.Id);
            Assert.That(explanation.Action, Is.Null, vector.Id);

            Assert.That(highlights.Status, Is.EqualTo(HintStatus.Available), vector.Id);
            Assert.That(highlights.TechniqueId, Is.EqualTo(vector.TechniqueId), vector.Id);
            Assert.That(highlights.TechniqueName, Is.EqualTo(catalogEntry.Name), vector.Id);
            Assert.That(highlights.Explanation, Is.EqualTo(catalogEntry.Explanation), vector.Id);
            Assert.That(highlights.Highlights.Keys, Is.EquivalentTo(expectedRoles), vector.Id);
            Assert.That(highlights.Highlights[HintHighlightRole.Pattern], Is.EqualTo(expectedPattern), vector.Id);
            if (expectedScope.Length > 0)
            {
                Assert.That(highlights.Highlights[HintHighlightRole.Scope], Is.EqualTo(expectedScope), vector.Id);
            }

            if (vector.ExpectedPosition is { } target)
            {
                Assert.That(highlights.Highlights[HintHighlightRole.Target], Is.EqualTo(new[] { target }), vector.Id);
            }

            if (expectedAffected.Length > 0)
            {
                Assert.That(highlights.Highlights[HintHighlightRole.Affected], Is.EqualTo(expectedAffected), vector.Id);
            }

            Assert.That(highlights.InvolvedCandidates, Is.EqualTo(expectedCandidates), vector.Id);
            Assert.That(highlights.Action, Is.Null, vector.Id);
            Assert.That(action.Status, Is.EqualTo(HintStatus.Available), vector.Id);
            Assert.That(action.TechniqueId, Is.EqualTo(vector.TechniqueId), vector.Id);
            Assert.That(action.BoardRevision, Is.EqualTo(100), vector.Id);

            if (vector.ExpectedPosition is { } placePosition)
            {
                Assert.That(action.Action, Is.TypeOf<HintAction.PlaceValue>(), vector.Id);
                var place = (HintAction.PlaceValue)action.Action!;
                Assert.That(place.Position, Is.EqualTo(placePosition), vector.Id);
                Assert.That(place.Value, Is.EqualTo(vector.ExpectedValue), vector.Id);
            }
            else
            {
                Assert.That(action.Action, Is.TypeOf<HintAction.RemoveCandidates>(), vector.Id);
                Assert.That(((HintAction.RemoveCandidates)action.Action!).Candidates, Is.EqualTo(expectedCandidates), vector.Id);
            }
        });
    }

    private static IEnumerable<TestCaseData> AcceptanceVectors()
    {
        foreach (var vector in Vectors)
        {
            yield return new TestCaseData(vector).SetName($"{vector.Id}_{vector.TechniqueId}_UsesValidSnapshot");
        }
    }

    private static IReadOnlyList<HintAcceptanceVector> Vectors =>
    [
        new(
            "LH-01",
            LogicalTechniqueId.NakedSingle,
            "971...25625...98.48.456....3.9.8.......19...85..62.3196......93..79...8.4....61..",
            [19],
            [],
            [3],
            [],
            new CellPosition(2, 1),
            3),
        new(
            "LH-02",
            LogicalTechniqueId.HiddenSingle,
            "97...8.56.56719.34.3.5..9...1..85.62.6219.5..54...731.6.5.7..931...3.6854.3856...",
            [3],
            [0, 1, 2, 3, 4, 5, 6, 7, 8],
            [3],
            [],
            new CellPosition(0, 3),
            3),
        new(
            "LH-03",
            LogicalTechniqueId.CageSingle,
            "8..97....1.6..8...2.74...5..2..9748......3..77.9.4.523...7..16.5.2....3...1.842.5",
            [63, 64, 65, 66, 67, 68, 69, 70, 71],
            [63, 64, 65, 66, 67, 68, 69, 70, 71],
            [9],
            [],
            new CellPosition(7, 5),
            9),
        new(
            "LH-04",
            LogicalTechniqueId.CageCombination,
            "8....6.726...4........38.9.......83...3..52499.4..........7...3.......2..5...1...",
            [0, 1, 2, 3, 4, 5, 6, 7, 8],
            [0, 1, 2, 3, 4, 5, 6, 7, 8],
            [1, 3, 4, 5, 9],
            [new CandidateElimination(new CellPosition(0, 1), 1)],
            null,
            null),
        new(
            "LH-05",
            LogicalTechniqueId.CageRegionIntersection,
            "...3.......6.......7..9...848..........4.....5..61....2..7.......1.6..4......1...",
            [33, 34, 35],
            [33, 34, 35, 42, 43, 44, 51, 52, 53],
            [1],
            [
                new CandidateElimination(new CellPosition(4, 6), 1),
                new CandidateElimination(new CellPosition(4, 7), 1),
                new CandidateElimination(new CellPosition(4, 8), 1)
            ],
            null,
            null),
        new(
            "LH-06",
            LogicalTechniqueId.RuleOf45,
            "9........3.4....5..8.....1.....8...46.......5.5....2..5.........9.......17......9",
            Enumerable.Range(0, 81).ToArray(),
            Enumerable.Range(0, 81).ToArray(),
            [2, 7],
            [
                new CandidateElimination(new CellPosition(5, 0), 7),
                new CandidateElimination(new CellPosition(7, 0), 2)
            ],
            null,
            null),
        new(
            "LH-07",
            LogicalTechniqueId.NakedPair,
            ".2...91..9.4..3.6.....25....3...........9.31......6........2...2....7..1.4...1...",
            [32, 41],
            [30, 31, 32, 39, 40, 41, 48, 49, 50],
            [4, 8],
            [
                new CandidateElimination(new CellPosition(3, 3), 4),
                new CandidateElimination(new CellPosition(3, 3), 8),
                new CandidateElimination(new CellPosition(3, 4), 4),
                new CandidateElimination(new CellPosition(3, 4), 8),
                new CandidateElimination(new CellPosition(4, 3), 4),
                new CandidateElimination(new CellPosition(4, 3), 8),
                new CandidateElimination(new CellPosition(5, 3), 4),
                new CandidateElimination(new CellPosition(5, 3), 8),
                new CandidateElimination(new CellPosition(5, 4), 4),
                new CandidateElimination(new CellPosition(5, 4), 8)
            ],
            null,
            null),
        new(
            "LH-08",
            LogicalTechniqueId.HiddenPair,
            ".....6...6...3..9............3...5.75.73..2.......5..3.5.....2..2.65..8.....2....",
            [44, 51],
            [33, 34, 35, 42, 43, 44, 51, 52, 53],
            [8, 9],
            [
                new CandidateElimination(new CellPosition(4, 8), 1),
                new CandidateElimination(new CellPosition(4, 8), 4),
                new CandidateElimination(new CellPosition(4, 8), 6),
                new CandidateElimination(new CellPosition(5, 6), 1),
                new CandidateElimination(new CellPosition(5, 6), 4),
                new CandidateElimination(new CellPosition(5, 6), 6)
            ],
            null,
            null),
        new(
            "LH-09",
            LogicalTechniqueId.NakedTriple,
            ".....93.8....1....8..5...4..5..769..........6....3......1........64........1.....",
            [30, 39, 48],
            [3, 12, 21, 30, 39, 48, 57, 66, 75],
            [2, 8, 9],
            [
                new CandidateElimination(new CellPosition(0, 3), 2),
                new CandidateElimination(new CellPosition(1, 3), 2),
                new CandidateElimination(new CellPosition(1, 3), 8),
                new CandidateElimination(new CellPosition(6, 3), 2),
                new CandidateElimination(new CellPosition(6, 3), 8),
                new CandidateElimination(new CellPosition(6, 3), 9)
            ],
            null,
            null)
    ];

    private static ValidatedPuzzle CreatePuzzle(string encodedGivens)
    {
        if (encodedGivens.Length != 81)
        {
            throw new ArgumentException("An acceptance vector must encode all 81 snapshot cells.", nameof(encodedGivens));
        }

        var givens = new Dictionary<PuzzleDefinitionPosition, int>();
        for (var index = 0; index < encodedGivens.Length; index++)
        {
            var digit = encodedGivens[index];
            if (digit == '.')
            {
                continue;
            }

            if (digit is < '1' or > '9')
            {
                throw new ArgumentException("An acceptance vector contains an invalid given.", nameof(encodedGivens));
            }

            givens.Add(new PuzzleDefinitionPosition(index / 9, index % 9), digit - '0');
        }

        var cages = Enumerable.Range(0, 9)
            .Select(row => new CageDefinition(
                45,
                Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))));
        var validation = new PuzzleStructureValidator().Validate(new PuzzleDefinition(givens, cages));
        Assert.That(validation.IsValid, Is.True, string.Join("; ", validation.Issues.Select(issue => issue.Message)));
        return validation.Puzzle!;
    }

    private static CellPosition[] ToPositions(IEnumerable<int> indexes) => indexes
        .Select(index => new CellPosition(index / 9, index % 9))
        .ToArray();

    public sealed record HintAcceptanceVector(
        string Id,
        LogicalTechniqueId TechniqueId,
        string Givens,
        int[] PatternIndexes,
        int[] ScopeIndexes,
        int[] RelevantDigits,
        CandidateElimination[] Eliminations,
        CellPosition? ExpectedPosition,
        int? ExpectedValue);
}



