using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Difficulty;

/// <summary>The normative cumulative difficulty profiles, catalog version 1.</summary>
public sealed class DifficultyProfileCatalog
{
    public const int CurrentVersion = 1;

    private static readonly LogicalTechniqueId[] EasyTechniques =
    [
        LogicalTechniqueId.NakedSingle,
        LogicalTechniqueId.HiddenSingle,
        LogicalTechniqueId.CageSingle
    ];

    private static readonly LogicalTechniqueId[] MediumTechniques =
    [.. EasyTechniques,
        LogicalTechniqueId.CageCombination];

    private static readonly LogicalTechniqueId[] HardTechniques =
    [.. MediumTechniques,
        LogicalTechniqueId.CageRegionIntersection,
        LogicalTechniqueId.RuleOf45];

    private static readonly IReadOnlyList<DifficultyProfile> Profiles = Array.AsReadOnly(new[]
    {
        new DifficultyProfile(DifficultyLevel.Easy, CurrentVersion, EasyTechniques),
        new DifficultyProfile(DifficultyLevel.Medium, CurrentVersion, MediumTechniques),
        new DifficultyProfile(DifficultyLevel.Hard, CurrentVersion, HardTechniques),
        new DifficultyProfile(DifficultyLevel.Expert, CurrentVersion,
            [.. HardTechniques,
                LogicalTechniqueId.NakedPair,
                LogicalTechniqueId.HiddenPair,
                LogicalTechniqueId.NakedTriple])
    });

    public int Version => CurrentVersion;

    public IReadOnlyList<DifficultyProfile> OrderedProfiles => Profiles;

    public DifficultyProfile GetProfile(DifficultyLevel level)
    {
        return Profiles.SingleOrDefault(profile => profile.Level == level)
            ?? throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown difficulty level.");
    }
}
