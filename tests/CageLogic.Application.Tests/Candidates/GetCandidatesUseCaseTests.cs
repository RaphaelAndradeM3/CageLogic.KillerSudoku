using CageLogic.Application.Candidates;
using CageLogic.Application.Moves;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Moves;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.Candidates;

public sealed class GetCandidatesUseCaseTests
{
    private readonly ApplyMoveUseCase _applyMove = new();
    private readonly GetCandidatesUseCase _getCandidates = new();

    [Test]
    public void Execute_AfterMoveAndClear_RecalculatesFromLatestBoard()
    {
        var board = CreatePuzzle().CreateBoard();
        var firstCell = new CellPosition(0, 0);
        var secondCell = new CellPosition(0, 1);
        var initialCandidates = CandidatesFor(_getCandidates.Execute(board), firstCell);

        var moveResult = _applyMove.Execute(board, new Move(secondCell, 9));
        var afterMove = _getCandidates.Execute(moveResult.Board);
        var candidatesAfterMove = CandidatesFor(afterMove, firstCell);

        var clearResult = _applyMove.Execute(moveResult.Board, Move.Clear(secondCell));
        var afterClear = _getCandidates.Execute(clearResult.Board);
        var candidatesAfterClear = CandidatesFor(afterClear, firstCell);

        Assert.That(initialCandidates.Values, Does.Contain(9));
        Assert.That(afterMove.Any(candidate => candidate.Position == secondCell), Is.False);
        Assert.That(candidatesAfterMove.Values, Is.EquivalentTo(new[] { 1 }));
        Assert.That(candidatesAfterClear.Values, Does.Contain(9));
        Assert.That(clearResult.Board.GetCell(secondCell).CurrentValue, Is.Null);
    }

    private static CandidateSet CandidatesFor(IReadOnlyList<CandidateSet> candidates, CellPosition position)
    {
        return candidates.Single(candidate => candidate.Position == position);
    }

    private static ValidatedPuzzle CreatePuzzle()
    {
        var cages = (from row in Enumerable.Range(0, 9)
                     from column in Enumerable.Range(0, 9)
                     where !((row == 0) && (column is 0 or 1))
                     select new CageDefinition(1, [new PuzzleDefinitionPosition(row, column)]))
            .ToList();
        cages.Add(new CageDefinition(10,
        [
            new PuzzleDefinitionPosition(0, 0),
            new PuzzleDefinitionPosition(0, 1)
        ]));
        var definition = new PuzzleDefinition(
            new Dictionary<PuzzleDefinitionPosition, int>(),
            cages);
        var result = new PuzzleStructureValidator().Validate(definition);
        Assert.That(result.IsValid, Is.True, string.Join("; ", result.Issues.Select(issue => issue.Message)));
        return result.Puzzle!;
    }
}
