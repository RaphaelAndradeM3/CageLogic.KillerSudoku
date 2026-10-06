namespace CageLogic.Domain.LogicalSteps;

/// <summary>Applies catalog v1 strategy order and returns the first deterministic available deduction.</summary>
public sealed class LogicalStepAnalyzer : ILogicalStepAnalyzer
{
    private readonly ILogicalTechnique[] _techniques;

    public LogicalStepAnalyzer(IEnumerable<ILogicalTechnique>? techniques = null)
    {
        _techniques = (techniques ?? CreateDefaultTechniques())
            .OrderBy(technique => technique.Id)
            .ToArray();
        if (_techniques.Select(technique => technique.Id).Distinct().Count() != _techniques.Length)
        {
            throw new ArgumentException("Only one strategy per logical technique identifier can be registered.", nameof(techniques));
        }
    }

    public LogicalStep? FindNextStep(
        LogicalState state,
        IReadOnlySet<LogicalTechniqueId>? allowedTechniques = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        foreach (var technique in _techniques)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (allowedTechniques is not null && !allowedTechniques.Contains(technique.Id))
            {
                continue;
            }

            var step = technique.FindStep(state, cancellationToken);
            if (step is not null)
            {
                return step;
            }
        }

        return null;
    }

    private static IEnumerable<ILogicalTechnique> CreateDefaultTechniques()
    {
        yield return new Techniques.NakedSingleTechnique();
        yield return new Techniques.HiddenSingleTechnique();
        yield return new Techniques.CageSingleTechnique();
        yield return new Techniques.CageCombinationTechnique();
        yield return new Techniques.CageRegionIntersectionTechnique();
        yield return new Techniques.RuleOf45Technique();
        yield return new Techniques.NakedPairTechnique();
        yield return new Techniques.HiddenPairTechnique();
        yield return new Techniques.NakedTripleTechnique();
    }
}
