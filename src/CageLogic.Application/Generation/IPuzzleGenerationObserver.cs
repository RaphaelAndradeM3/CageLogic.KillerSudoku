namespace CageLogic.Application.Generation;

/// <summary>Optional diagnostics sink for generation timings and rejected candidate reasons.</summary>
public interface IPuzzleGenerationObserver
{
    void StageCompleted(PuzzleGenerationStageMeasurement measurement);

    void CandidateRejected(int attempt, PuzzleGenerationRejectionReason reason);
}
