namespace CageLogic.Application.Generation;

/// <summary>Explicit positive limits for one puzzle generation request.</summary>
public sealed class GenerationBudget
{
    public GenerationBudget(int maxAttempts, TimeSpan timeLimit)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "The attempt budget must be positive.");
        }

        if (timeLimit <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeLimit), timeLimit, "The time budget must be positive.");
        }

        MaxAttempts = maxAttempts;
        TimeLimit = timeLimit;
    }

    public int MaxAttempts { get; }

    public TimeSpan TimeLimit { get; }
}
