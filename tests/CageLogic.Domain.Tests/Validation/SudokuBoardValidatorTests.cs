using CageLogic.Domain.Board;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Tests.Support;
using CageLogic.Domain.Validation;

namespace CageLogic.Domain.Tests.Validation;

public sealed class SudokuBoardValidatorTests
{
    private readonly PuzzleStructureValidator _structureValidator = new();
    private readonly SudokuBoardValidator _validator = new();

    [Test]
    public void Validate_RepeatedValueInRow_ReportsRowConflict()
    {
        var result = ValidateWithGivens((0, 0, 5), (0, 4, 5));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.IsSolved, Is.False);
        Assert.That(result.Conflicts.Any(conflict => conflict.Rule == ValueConflictRule.DuplicateRow), Is.True);
    }

    [Test]
    public void Validate_RepeatedValueInColumn_ReportsColumnConflict()
    {
        var result = ValidateWithGivens((0, 0, 5), (4, 0, 5));

        Assert.That(result.Conflicts.Any(conflict => conflict.Rule == ValueConflictRule.DuplicateColumn), Is.True);
    }

    [Test]
    public void Validate_RepeatedValueInBlock_ReportsBlockConflict()
    {
        var result = ValidateWithGivens((0, 0, 5), (1, 1, 5));

        Assert.That(result.Conflicts.Any(conflict => conflict.Rule == ValueConflictRule.DuplicateBlock), Is.True);
    }

    [Test]
    public void Validate_RepeatedValueInCage_ReportsCageConflict()
    {
        var puzzle = PuzzleTestData.WithCage(
            [(0, 0), (0, 1)],
            targetSum: 4,
            givens: PuzzleTestData.Givens((0, 0, 2), (0, 1, 2)));
        var result = Validate(puzzle);

        Assert.That(result.Conflicts.Any(conflict => conflict.Rule == ValueConflictRule.DuplicateCage), Is.True);
    }

    [Test]
    public void Validate_CagePartialSumAlreadyUnreachable_ReportsCageTargetConflict()
    {
        var puzzle = PuzzleTestData.WithCage(
            [(0, 0), (0, 1), (1, 0)],
            targetSum: 9,
            givens: PuzzleTestData.Givens((0, 0, 8), (0, 1, 1)));
        var result = Validate(puzzle);

        Assert.That(result.Conflicts.Any(conflict => conflict.Rule == ValueConflictRule.CageTargetUnreachable), Is.True);
    }

    [Test]
    public void Validate_PartialConflictFreeBoard_IsValidAndIncomplete()
    {
        var board = CreateValidatedPuzzle(PuzzleTestData.CreateDefinition()).CreateBoard();

        var result = _validator.Validate(board);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.IsSolved, Is.False);
    }

    [Test]
    public void Validate_CompleteConflictFreeBoard_IsSolved()
    {
        var solvedGivens = (from row in Enumerable.Range(0, 9)
                            from column in Enumerable.Range(0, 9)
                            let value = (row * 3 + row / 3 + column) % 9 + 1
                            select (row, column, value)).ToArray();
        var puzzle = PuzzleTestData.CreateDefinition(givens: PuzzleTestData.Givens(solvedGivens));
        var board = CreateValidatedPuzzle(puzzle).CreateBoard();

        var result = _validator.Validate(board);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsComplete, Is.True);
        Assert.That(result.IsSolved, Is.True);
    }

    private BoardValidationResult ValidateWithGivens(params (int Row, int Column, int Value)[] givens)
    {
        return Validate(PuzzleTestData.CreateDefinition(givens: PuzzleTestData.Givens(givens)));
    }

    private BoardValidationResult Validate(PuzzleDefinition definition)
    {
        return _validator.Validate(CreateValidatedPuzzle(definition).CreateBoard());
    }

    private ValidatedPuzzle CreateValidatedPuzzle(PuzzleDefinition definition)
    {
        var structure = _structureValidator.Validate(definition);
        Assert.That(structure.IsValid, Is.True, string.Join("; ", structure.Issues.Select(issue => issue.Message)));
        return structure.Puzzle!;
    }
}
