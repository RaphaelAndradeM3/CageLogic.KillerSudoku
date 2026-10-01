using CageLogic.Application.Moves;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Moves;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.Moves;

public sealed class ApplyMoveUseCaseTests
{
    private readonly ApplyMoveUseCase _useCase = new();
    private readonly SudokuBoard _initialBoard = CreatePuzzle().CreateBoard();

    [Test]
    public void Execute_InsertValue_ReturnsNewBoardAndValidation()
    {
        var position = new CellPosition(0, 1);

        var result = _useCase.Execute(_initialBoard, new Move(position, 7));

        Assert.That(result.IsApplied, Is.True);
        Assert.That(result.RejectionReason, Is.Null);
        Assert.That(result.Board, Is.Not.SameAs(_initialBoard));
        Assert.That(result.Board.GetCell(position).PlayerValue, Is.EqualTo(7));
        Assert.That(result.Validation.IsValid, Is.True);
        Assert.That(_initialBoard.GetCell(position).CurrentValue, Is.Null);
    }

    [Test]
    public void Execute_ReplacePlayerValue_UsesNewValue()
    {
        var position = new CellPosition(0, 1);
        var first = _useCase.Execute(_initialBoard, new Move(position, 3));

        var replacement = _useCase.Execute(first.Board, new Move(position, 8));

        Assert.That(replacement.IsApplied, Is.True);
        Assert.That(replacement.Board.GetCell(position).PlayerValue, Is.EqualTo(8));
    }

    [Test]
    public void Execute_ClearPlayerValue_RemovesIt()
    {
        var position = new CellPosition(0, 1);
        var entered = _useCase.Execute(_initialBoard, new Move(position, 4));

        var cleared = _useCase.Execute(entered.Board, Move.Clear(position));

        Assert.That(cleared.IsApplied, Is.True);
        Assert.That(cleared.Board.GetCell(position).PlayerValue, Is.Null);
        Assert.That(cleared.Board.GetCell(position).CurrentValue, Is.Null);
    }

    [TestCase(6)]
    [TestCase(null)]
    public void Execute_EditGivenValue_ReturnsExplicitRejectionAndPreservesGiven(int? value)
    {
        var position = new CellPosition(0, 0);

        var result = _useCase.Execute(_initialBoard, new Move(position, value));

        Assert.That(result.IsApplied, Is.False);
        Assert.That(result.RejectionReason, Is.EqualTo(MoveRejectionReason.GivenValueIsFixed));
        Assert.That(result.Board, Is.SameAs(_initialBoard));
        Assert.That(result.Board.GetCell(position).GivenValue, Is.EqualTo(5));
        Assert.That(result.Board.GetCell(position).PlayerValue, Is.Null);
    }

    private static ValidatedPuzzle CreatePuzzle()
    {
        var cages = (from row in Enumerable.Range(0, 9)
                     from column in Enumerable.Range(0, 9)
                     where (row, column) is not (0, 0) and not (0, 1) and not (0, 2)
                     select new CageDefinition(1, [new PuzzleDefinitionPosition(row, column)]))
            .ToList();
        cages.Add(new CageDefinition(5, [new PuzzleDefinitionPosition(0, 0)]));
        cages.Add(new CageDefinition(9,
        [
            new PuzzleDefinitionPosition(0, 1),
            new PuzzleDefinitionPosition(0, 2)
        ]));
        var definition = new PuzzleDefinition(
            new Dictionary<PuzzleDefinitionPosition, int> { [new(0, 0)] = 5 },
            cages);
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }
}
