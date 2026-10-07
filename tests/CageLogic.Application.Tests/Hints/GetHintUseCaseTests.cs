using CageLogic.Application.Hints;
using CageLogic.Application.Solving;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

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
}
