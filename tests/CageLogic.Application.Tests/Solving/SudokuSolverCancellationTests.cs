using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Validation;

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
    public void FindSolutions_CancelledDuringSearch_ThrowsOperationCanceledException()
    {
        using var cancellation = new CancellationTokenSource();
        var puzzle = SolverPuzzleFixtures.CreateAmbiguousPuzzle();

        Assert.That(
            () => new SudokuSolver(
                    new CancellingCandidateCalculator(cancellation),
                    new SudokuBoardValidator())
                .FindSolutions(puzzle, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private sealed class CancellingCandidateCalculator(CancellationTokenSource cancellation) : ICandidateCalculator
    {
        private readonly CandidateCalculator _inner = new();
        private int _calls;

        public IReadOnlyList<CandidateSet> Calculate(SudokuBoard board)
        {
            var candidates = _inner.Calculate(board);
            if (Interlocked.Increment(ref _calls) == 2)
            {
                cancellation.Cancel();
            }

            return candidates;
        }
    }
}
