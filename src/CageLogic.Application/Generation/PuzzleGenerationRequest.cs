using CageLogic.Application.Difficulty;

namespace CageLogic.Application.Generation;

/// <summary>A seeded or unseeded request with an explicit difficulty and resource budget.</summary>
public sealed class PuzzleGenerationRequest
{
    public PuzzleGenerationRequest(DifficultyLevel difficulty, GenerationBudget budget, int? seed = null)
    {
        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        ArgumentNullException.ThrowIfNull(budget);
        Difficulty = difficulty;
        Budget = budget;
        Seed = seed;
    }

    public DifficultyLevel Difficulty { get; }

    public GenerationBudget Budget { get; }

    public int? Seed { get; }
}
