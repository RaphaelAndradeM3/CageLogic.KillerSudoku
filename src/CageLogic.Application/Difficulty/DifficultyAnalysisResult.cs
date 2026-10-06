using System.Collections.ObjectModel;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Difficulty;

/// <summary>The complete deterministic technique trace and its versioned difficulty classification.</summary>
public sealed class DifficultyAnalysisResult
{
    public DifficultyAnalysisResult(
        DifficultyAnalysisStatus status,
        DifficultyLevel? level,
        int catalogVersion,
        IEnumerable<LogicalTechniqueId> techniqueTrace)
    {
        ArgumentNullException.ThrowIfNull(techniqueTrace);
        if (catalogVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(catalogVersion));
        }

        if ((status == DifficultyAnalysisStatus.Classified) != level.HasValue)
        {
            throw new ArgumentException("A difficulty level is present exactly when analysis is classified.", nameof(level));
        }

        if (!Enum.IsDefined(status) || (level.HasValue && !Enum.IsDefined(level.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        var trace = techniqueTrace.ToArray();
        if (trace.Any(technique => !Enum.IsDefined(technique)))
        {
            throw new ArgumentException("The trace contains an unknown logical technique identifier.", nameof(techniqueTrace));
        }

        Status = status;
        Level = level;
        CatalogVersion = catalogVersion;
        TechniqueTrace = new ReadOnlyCollection<LogicalTechniqueId>(trace);
    }

    public DifficultyAnalysisStatus Status { get; }

    public bool IsClassified => Status == DifficultyAnalysisStatus.Classified;

    public DifficultyLevel? Level { get; }

    public int CatalogVersion { get; }

    public IReadOnlyList<LogicalTechniqueId> TechniqueTrace { get; }
}
