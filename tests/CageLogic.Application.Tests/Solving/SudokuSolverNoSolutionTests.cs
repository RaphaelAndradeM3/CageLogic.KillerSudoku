using CageLogic.Application.Solving;

namespace CageLogic.Application.Tests.Solving;

public sealed class SudokuSolverNoSolutionTests
{
    [Test]
    public void FindSolutions_ContradictoryGivens_ReturnsNoSolution()
    {
        var puzzle = SolverPuzzleFixtures.CreateUniquePuzzle(
            SolverPuzzleFixtures.Givens((0, 0, 1), (0, 1, 1)));

        var result = new SudokuSolver().FindSolutions(puzzle);

        Assert.That(result.Multiplicity, Is.EqualTo(SolutionMultiplicity.NoSolution));
        Assert.That(result.SolutionsFoundUpToLimit, Is.Zero);
        Assert.That(result.FirstSolution, Is.Null);
    }
}
