using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Difficulty;

/// <summary>Classifies a puzzle using the least advanced profile that completes its logical trace.</summary>
public sealed class DifficultyAnalyzer : IDifficultyAnalyzer
{
    private readonly DifficultyProfileCatalog _catalog;
    private readonly ILogicalStepAnalyzer _stepAnalyzer;
    private readonly SudokuBoardValidator _boardValidator;

    public DifficultyAnalyzer()
        : this(new DifficultyProfileCatalog(), new LogicalStepAnalyzer(), new SudokuBoardValidator())
    {
    }

    public DifficultyAnalyzer(
        DifficultyProfileCatalog catalog,
        ILogicalStepAnalyzer stepAnalyzer,
        SudokuBoardValidator boardValidator)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(stepAnalyzer);
        ArgumentNullException.ThrowIfNull(boardValidator);
        _catalog = catalog;
        _stepAnalyzer = stepAnalyzer;
        _boardValidator = boardValidator;
    }

    public DifficultyAnalysisResult Analyze(ValidatedPuzzle puzzle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        IReadOnlyList<LogicalTechniqueId> lastTrace = Array.Empty<LogicalTechniqueId>();

        foreach (var profile in _catalog.OrderedProfiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = LogicalState.Create(puzzle.CreateBoard());
            var trace = new List<LogicalTechniqueId>();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (state.Candidates.Count == 0)
                {
                    if (_boardValidator.Validate(state.Board).IsSolved)
                    {
                        return new DifficultyAnalysisResult(
                            DifficultyAnalysisStatus.Classified,
                            profile.Level,
                            profile.CatalogVersion,
                            trace);
                    }

                    break;
                }

                var step = _stepAnalyzer.FindNextStep(state, profile.Techniques, cancellationToken);
                if (step is null)
                {
                    break;
                }

                state = state.Apply(step);
                trace.Add(step.TechniqueId);
            }

            lastTrace = trace;
        }

        return new DifficultyAnalysisResult(
            DifficultyAnalysisStatus.Unclassifiable,
            level: null,
            DifficultyProfileCatalog.CurrentVersion,
            lastTrace);
    }
}
