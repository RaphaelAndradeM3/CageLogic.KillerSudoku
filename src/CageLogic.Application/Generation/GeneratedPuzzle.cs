using CageLogic.Application.Difficulty;
using CageLogic.Application.Solving;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Generation;

/// <summary>A fully validated, unique puzzle accepted at the requested difficulty.</summary>
public sealed class GeneratedPuzzle
{
    public GeneratedPuzzle(
        ValidatedPuzzle puzzle,
        SolutionGrid solution,
        DifficultyAnalysisResult difficulty,
        DifficultyLevel requestedDifficulty,
        int? seed,
        int attempts,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        ArgumentNullException.ThrowIfNull(solution);
        ArgumentNullException.ThrowIfNull(difficulty);
        if (!Enum.IsDefined(requestedDifficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedDifficulty));
        }

        if (!difficulty.IsClassified || difficulty.Level != requestedDifficulty)
        {
            throw new ArgumentException("The difficulty analysis must match the requested level.", nameof(difficulty));
        }

        if (attempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed));
        }

        if (!new SudokuBoardValidator().Validate(solution.ToBoard(puzzle)).IsSolved)
        {
            throw new ArgumentException("The solution is not valid for the supplied puzzle.", nameof(solution));
        }
        Puzzle = puzzle;
        Solution = solution;
        Difficulty = difficulty;
        RequestedDifficulty = requestedDifficulty;
        Seed = seed;
        Attempts = attempts;
        Elapsed = elapsed;
    }

    public ValidatedPuzzle Puzzle { get; }

    public SolutionGrid Solution { get; }

    public DifficultyAnalysisResult Difficulty { get; }

    public DifficultyLevel RequestedDifficulty { get; }

    public int? Seed { get; }

    public int Attempts { get; }

    public TimeSpan Elapsed { get; }
}
