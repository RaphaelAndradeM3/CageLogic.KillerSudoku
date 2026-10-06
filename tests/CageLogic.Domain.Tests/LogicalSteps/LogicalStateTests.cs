using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Domain.Tests.LogicalSteps;

public sealed class LogicalStateTests
{
    [Test]
    public void Apply_PlacementRecalculatesCandidatesAndPreservesEarlierEliminations()
    {
        var puzzle = CreateOpenPuzzle();
        var board = puzzle.CreateBoard();
        var candidates = new CandidateCalculator().Calculate(board).ToDictionary(set => set.Position);
        candidates[new CellPosition(0, 0)] = new CandidateSet(new CellPosition(0, 0), [1]);
        var state = new LogicalState(board, candidates.Values);

        var afterElimination = state.Apply(new LogicalStep(
            LogicalTechniqueId.NakedPair,
            eliminations: [new CandidateElimination(new CellPosition(4, 4), 7)]));
        var afterPlacement = afterElimination.Apply(new LogicalStep(
            LogicalTechniqueId.NakedSingle,
            new LogicalPlacement(new CellPosition(0, 0), 1)));

        Assert.That(afterPlacement.Board.GetCell(new CellPosition(0, 0)).CurrentValue, Is.EqualTo(1));
        Assert.That(afterPlacement.GetCandidates(new CellPosition(4, 4)).Values, Does.Not.Contain(7));
        Assert.That(state.Board.GetCell(new CellPosition(0, 0)).CurrentValue, Is.Null);
        Assert.That(state.GetCandidates(new CellPosition(4, 4)).Values, Does.Contain(7));
    }

    private static ValidatedPuzzle CreateOpenPuzzle()
    {
        var cages = Enumerable.Range(0, 9).Select(row => new CageDefinition(45,
            Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))));
        var result = new PuzzleStructureValidator().Validate(new PuzzleDefinition(null, cages));
        Assert.That(result.IsValid, Is.True);
        return result.Puzzle!;
    }
}
