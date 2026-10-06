namespace CageLogic.Domain.LogicalSteps;

/// <summary>One logical deduction strategy with a stable catalog identifier.</summary>
public interface ILogicalTechnique
{
    LogicalTechniqueId Id { get; }

    LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default);
}
