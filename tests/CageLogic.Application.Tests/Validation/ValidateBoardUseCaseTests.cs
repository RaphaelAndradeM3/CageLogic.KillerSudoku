using CageLogic.Application.Validation;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Tests.Validation;

public sealed class ValidateBoardUseCaseTests
{
    private readonly ValidateBoardUseCase _useCase = new();

    [Test]
    public void Execute_ValidPartialBoard_ReturnsValidationWithoutChangingBoard()
    {
        var board = CreatePuzzle(new Dictionary<PuzzleDefinitionPosition, int>
        {
            [new(0, 0)] = 4
        }).CreateBoard();
        var originalCells = board.Cells.ToArray();

        var result = _useCase.Execute(board);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsComplete, Is.False);
        Assert.That(result.IsSolved, Is.False);
        Assert.That(result.Conflicts, Is.Empty);
        Assert.That(board.Cells, Is.EquivalentTo(originalCells));
        Assert.That(board.GetCell(new CellPosition(0, 0)).GivenValue, Is.EqualTo(4));
    }

    [Test]
    public void Execute_BoardWithDuplicateRowValues_ReturnsConflictWithoutChangingBoard()
    {
        var board = CreatePuzzle(new Dictionary<PuzzleDefinitionPosition, int>
        {
            [new(0, 0)] = 4,
            [new(0, 4)] = 4
        }).CreateBoard();
        var originalCells = board.Cells.ToArray();

        var result = _useCase.Execute(board);

        var rowConflict = result.Conflicts.Single(conflict => conflict.Rule == ValueConflictRule.DuplicateRow);
        Assert.That(result.IsValid, Is.False);
        Assert.That(rowConflict.RepeatedValue, Is.EqualTo(4));
        Assert.That(rowConflict.Positions, Is.EquivalentTo(new[]
        {
            new CellPosition(0, 0),
            new CellPosition(0, 4)
        }));
        Assert.That(board.Cells, Is.EquivalentTo(originalCells));
        Assert.That(board.GetCell(new CellPosition(0, 0)).CurrentValue, Is.EqualTo(4));
        Assert.That(board.GetCell(new CellPosition(0, 4)).CurrentValue, Is.EqualTo(4));
    }

    [Test]
    public void Execute_NullBoard_ThrowsArgumentNullException()
    {
        Assert.That(() => _useCase.Execute(null!), Throws.ArgumentNullException);
    }

    private static ValidatedPuzzle CreatePuzzle(IReadOnlyDictionary<PuzzleDefinitionPosition, int> givens)
    {
        var cages = from row in Enumerable.Range(0, 9)
                    from column in Enumerable.Range(0, 9)
                    let position = new PuzzleDefinitionPosition(row, column)
                    let target = givens.TryGetValue(position, out var value) ? value : 1
                    select new CageDefinition(target, [position]);
        var definition = new PuzzleDefinition(givens, cages);
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }
}
