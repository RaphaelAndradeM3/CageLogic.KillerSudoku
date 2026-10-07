namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class CageSingleTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.CageSingle;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new List<LogicalStep>();
        foreach (var cage in state.Board.Cages.OrderBy(cage => cage.Positions.Min(LogicalStepOrdering.PositionIndex)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assignments = state.GetCageAssignments(cage, cancellationToken);
            if (!assignments.HasAssignments)
            {
                continue;
            }

            foreach (var (position, values) in assignments.ValuesByPosition.OrderBy(pair => LogicalStepOrdering.PositionIndex(pair.Key)))
            {
                if (values.Count == 1)
                {
                    steps.Add(new LogicalStep(
                        Id,
                        new LogicalPlacement(position, values.Single()),
                        relatedPositions: cage.Positions,
                        evidence: new LogicalStepEvidence(
                            cage.Positions,
                            cage.Positions,
                            [values.Single()],
                            LogicalScopeContext.ForCage(cage.TargetSum),
                            [new LogicalPatternCandidate(position, values.Single())])));
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }
}
