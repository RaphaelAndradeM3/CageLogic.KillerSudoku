namespace CageLogic.Application.Generation;

/// <summary>Either a fully accepted generated puzzle or an explicit budget exhaustion.</summary>
public sealed class PuzzleGenerationResult
{
    private PuzzleGenerationResult(GeneratedPuzzle? generatedPuzzle, UnavailableReason? unavailableReason, int attempts, TimeSpan elapsed)
    {
        if ((generatedPuzzle is null) == (unavailableReason is null))
        {
            throw new ArgumentException("A generation result must contain exactly one success or unavailable outcome.");
        }

        if (attempts < 0 || elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(attempts));
        }

        GeneratedPuzzle = generatedPuzzle;
        UnavailableReason = unavailableReason;
        Attempts = attempts;
        Elapsed = elapsed;
    }

    public bool IsSuccess => GeneratedPuzzle is not null;

    public GeneratedPuzzle? GeneratedPuzzle { get; }

    public UnavailableReason? UnavailableReason { get; }

    public int Attempts { get; }

    public TimeSpan Elapsed { get; }

    public static PuzzleGenerationResult Success(GeneratedPuzzle generatedPuzzle)
    {
        ArgumentNullException.ThrowIfNull(generatedPuzzle);
        return new PuzzleGenerationResult(generatedPuzzle, null, generatedPuzzle.Attempts, generatedPuzzle.Elapsed);
    }

    public static PuzzleGenerationResult Unavailable(UnavailableReason reason, int attempts, TimeSpan elapsed)
    {
        if (!Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new PuzzleGenerationResult(null, reason, attempts, elapsed);
    }
}
