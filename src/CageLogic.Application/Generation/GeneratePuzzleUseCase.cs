namespace CageLogic.Application.Generation;

/// <summary>Runs CPU-bound puzzle generation away from the UI thread.</summary>
public sealed class GeneratePuzzleUseCase
{
    private readonly PuzzleGenerator _generator;

    public GeneratePuzzleUseCase()
        : this(new PuzzleGenerator())
    {
    }

    public GeneratePuzzleUseCase(PuzzleGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        _generator = generator;
    }

    public Task<PuzzleGenerationResult> ExecuteAsync(
        PuzzleGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return Task.Run(() => _generator.Generate(request, cancellationToken), cancellationToken);
    }
}
