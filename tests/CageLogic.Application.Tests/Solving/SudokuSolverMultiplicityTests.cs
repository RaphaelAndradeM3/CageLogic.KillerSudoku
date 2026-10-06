using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Tests.Solving;

public sealed class SudokuSolverMultiplicityTests
{
    [Test]
    public void FindSolutions_ForcedBySingletonCages_ReturnsCompleteUniqueSolution()
    {
        var puzzle = SolverPuzzleFixtures.CreateUniquePuzzle();

        var result = new SudokuSolver().FindSolutions(puzzle);

        Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Unique));
        Assert.That(result.SolutionsFoundUpToLimit, Is.EqualTo(1));
        Assert.That(result.FirstSolution, Is.Not.Null);
        Assert.That(new SudokuBoardValidator().Validate(result.FirstSolution!.ToBoard(puzzle)).IsSolved, Is.True);
    }

    [Test]
    public void FindSolutions_RowCages_ReturnsMultipleAfterTwoSolutions()
    {
        var puzzle = SolverPuzzleFixtures.CreateAmbiguousPuzzle();

        var result = new SudokuSolver().FindSolutions(puzzle);

        Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.Multiple));
        Assert.That(result.SolutionsFoundUpToLimit, Is.EqualTo(2));
        Assert.That(result.FirstSolution, Is.Not.Null);
        Assert.That(new SudokuBoardValidator().Validate(result.FirstSolution!.ToBoard(puzzle)).IsSolved, Is.True);
    }
}
