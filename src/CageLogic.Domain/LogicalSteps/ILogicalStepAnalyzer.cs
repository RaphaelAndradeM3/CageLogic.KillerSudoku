namespace CageLogic.Domain.LogicalSteps;

/// <summary>Finds the next supported deterministic logical deduction.</summary>
public interface ILogicalStepAnalyzer
{
    LogicalStep? FindNextStep(
        LogicalState state,
        IReadOnlySet<LogicalTechniqueId>? allowedTechniques = null,
        CancellationToken cancellationToken = default);
}
