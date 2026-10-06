using CageLogic.Application.Solving;

namespace CageLogic.Application.Tests.Solving;

public sealed class SudokuSolverCancellationTests
{
    [Test]
    public void FindSolutions_PreCancelledToken_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.That(
            () => new SudokuSolver().FindSolutions(SolverPuzzleFixtures.CreateAmbiguousPuzzle(), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public async Task FindSolutions_CancelledDuringSearch_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        var puzzle = SolverPuzzleFixtures.CreateAmbiguousPuzzle();
        var search = Task.Run(() => new SudokuSolver().FindSolutions(puzzle, cancellation.Token));
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(1));

        Assert.That(
            async () => await search,
            Throws.InstanceOf<OperationCanceledException>());
    }
}
