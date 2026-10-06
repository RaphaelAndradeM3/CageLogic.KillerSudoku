using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Solving;

/// <summary>Runs solution search on a worker thread and preserves caller cancellation semantics.</summary>
public sealed class FindSolutionsUseCase
{
    private readonly SudokuSolver _solver;

    public FindSolutionsUseCase()
        : this(new SudokuSolver())
    {
    }

    public FindSolutionsUseCase(SudokuSolver solver)
    {
        ArgumentNullException.ThrowIfNull(solver);
        _solver = solver;
    }

    public Task<SolutionSearchResult> ExecuteAsync(
        ValidatedPuzzle puzzle,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        return Task.Run(() => _solver.FindSolutions(puzzle, cancellationToken), cancellationToken);
    }
}
