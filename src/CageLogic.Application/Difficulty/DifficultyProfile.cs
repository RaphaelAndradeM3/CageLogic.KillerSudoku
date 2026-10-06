using System.Collections.Frozen;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Difficulty;

/// <summary>A cumulative, versioned set of logical techniques allowed for one level.</summary>
public sealed class DifficultyProfile
{
    internal DifficultyProfile(DifficultyLevel level, int catalogVersion, IEnumerable<LogicalTechniqueId> techniques)
    {
        Level = level;
        CatalogVersion = catalogVersion;
        OrderedTechniques = Array.AsReadOnly(techniques.Distinct().Order().ToArray());
        Techniques = OrderedTechniques.ToFrozenSet();
    }

    public DifficultyLevel Level { get; }

    public int CatalogVersion { get; }

    public IReadOnlyList<LogicalTechniqueId> OrderedTechniques { get; }

    public IReadOnlySet<LogicalTechniqueId> Techniques { get; }
}
