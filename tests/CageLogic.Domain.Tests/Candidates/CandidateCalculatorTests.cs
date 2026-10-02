using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Tests.Support;

namespace CageLogic.Domain.Tests.Candidates;

public sealed class CandidateCalculatorTests
{
    private readonly CandidateCalculator _calculator = new();

    [Test]
    public void Calculate_RemovesDigitsAlreadyPresentInRow()
    {
        var candidates = CalculatePairCage(PuzzleTestData.Givens((0, 4, 1)));

        Assert.That(candidates.Values.Contains(1), Is.False);
    }

    [Test]
    public void Calculate_RemovesDigitsAlreadyPresentInColumn()
    {
        var candidates = CalculatePairCage(PuzzleTestData.Givens((4, 0, 2)));

        Assert.That(candidates.Values.Contains(2), Is.False);
    }

    [Test]
    public void Calculate_RemovesDigitsAlreadyPresentInBlock()
    {
        var candidates = CalculatePairCage(PuzzleTestData.Givens((1, 2, 3)));

        Assert.That(candidates.Values.Contains(3), Is.False);
    }

    [Test]
    public void Calculate_RequiresDistinctDigitsToCompleteCageTarget()
    {
        var candidates = CalculatePairCage(PuzzleTestData.Givens());

        Assert.That(candidates.Values.Contains(5), Is.False);
        Assert.That(candidates.Values.Contains(4), Is.True);
        Assert.That(candidates.Values.Contains(6), Is.True);
    }

    [Test]
    public void Calculate_UsesDigitsFromEveryPositionInTheCage()
    {
        var cage = new (int Row, int Column)[] { (0, 0), (0, 1), (1, 1), (2, 1), (3, 1) };
        var puzzle = PuzzleTestData.WithCage(
            cage,
            targetSum: 30,
            givens: PuzzleTestData.Givens((3, 1, 9)));
        var candidates = CalculateAt(puzzle, new CellPosition(0, 0));

        Assert.That(candidates.Values.Contains(9), Is.False);
        Assert.That(candidates.Values.Contains(8), Is.True);
    }

    [Test]
    public void Calculate_DoesNotSearchForAFullBoardSolution()
    {
        var puzzle = PuzzleTestData.WithCage(
            [(0, 0), (0, 1)],
            targetSum: 10,
            givens: PuzzleTestData.Givens((5, 5, 4), (5, 6, 4)));
        var candidates = CalculateAt(puzzle, new CellPosition(0, 0));

        Assert.That(candidates.Values.Contains(9), Is.True);
    }

    [Test]
    public void Calculate_DoesNotReturnFilledCells()
    {
        var puzzle = PuzzleTestData.CreateDefinition(givens: PuzzleTestData.Givens((0, 0, 1)));
        var board = CreateBoard(puzzle);

        var candidates = _calculator.Calculate(board);

        Assert.That(candidates.Any(candidate => candidate.Position == new CellPosition(0, 0)), Is.False);
    }

    [Test]
    public void Calculate_ReturnsEmptySetWhenLocalRestrictionsLeaveNoDigit()
    {
        var givens = PuzzleTestData.Givens(
            (0, 0, 1), (0, 1, 2), (0, 2, 3), (0, 3, 4),
            (0, 4, 5), (0, 5, 6), (0, 6, 7), (0, 7, 8),
            (1, 8, 9));
        var puzzle = PuzzleTestData.CreateDefinition(givens: givens);
        var candidates = CalculateAt(puzzle, new CellPosition(0, 8));

        Assert.That(candidates.Values, Is.Empty);
    }

    private CandidateSet CalculatePairCage(IReadOnlyDictionary<PuzzleDefinitionPosition, int> givens)
    {
        var puzzle = PuzzleTestData.WithCage([(0, 0), (0, 1)], targetSum: 10, givens);
        return CalculateAt(puzzle, new CellPosition(0, 0));
    }

    private CandidateSet CalculateAt(PuzzleDefinition puzzle, CellPosition position)
    {
        var board = CreateBoard(puzzle);
        return _calculator.Calculate(board).Single(candidate => candidate.Position == position);
    }

    private static SudokuBoard CreateBoard(PuzzleDefinition definition)
    {
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!.CreateBoard();
    }
}
