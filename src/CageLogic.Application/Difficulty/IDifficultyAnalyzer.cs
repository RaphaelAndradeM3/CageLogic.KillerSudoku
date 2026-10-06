using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Difficulty;

public interface IDifficultyAnalyzer
{
    DifficultyAnalysisResult Analyze(ValidatedPuzzle puzzle, CancellationToken cancellationToken = default);
}
