using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class CageCombinationTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.CageCombination;

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

            foreach (var position in cage.Positions.OrderBy(LogicalStepOrdering.PositionIndex))
            {
                if (state.Board.GetCell(position).CurrentValue.HasValue)
                {
                    continue;
                }

                foreach (var candidate in state.GetCandidates(position).Values.Order())
                {
                    if (!assignments.ValuesByPosition[position].Contains(candidate))
                    {
                        steps.Add(new LogicalStep(
                            Id,
                            eliminations: [new CandidateElimination(position, candidate)],
                            relatedPositions: cage.Positions));
                    }
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }
}
