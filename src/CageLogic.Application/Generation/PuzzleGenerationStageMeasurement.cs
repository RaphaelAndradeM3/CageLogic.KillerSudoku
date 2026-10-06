namespace CageLogic.Application.Generation;

public sealed record PuzzleGenerationStageMeasurement(
    int Attempt,
    PuzzleGenerationStage Stage,
    TimeSpan Elapsed);
