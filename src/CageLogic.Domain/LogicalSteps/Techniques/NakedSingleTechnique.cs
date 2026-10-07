namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class NakedSingleTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.NakedSingle;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        cancellationToken.ThrowIfCancellationRequested();
        return state.Candidates
            .Where(candidates => candidates.Values.Count == 1)
            .Select(candidates => new LogicalStep(
                Id,
                new LogicalPlacement(candidates.Position, candidates.Values.Single()),
                evidence: new LogicalStepEvidence(
                    [candidates.Position],
                    relevantDigits: candidates.Values)))
            .FirstOrDefault();
    }
}
