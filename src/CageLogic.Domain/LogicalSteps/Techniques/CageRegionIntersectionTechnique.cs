using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class CageRegionIntersectionTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.CageRegionIntersection;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var cagePositions = state.Board.Cages.ToDictionary(cage => cage, cage => cage.Positions.ToHashSet());
        var steps = new List<LogicalStep>();
        foreach (var cage in state.Board.Cages.OrderBy(cage => cage.Positions.Min(LogicalStepOrdering.PositionIndex)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assignments = state.GetCageAssignments(cage, cancellationToken);
            if (!assignments.HasAssignments)
            {
                continue;
            }

            foreach (var (digit, possiblePositions) in assignments.PositionsByValue.OrderBy(pair => pair.Key))
            {
                foreach (var region in SudokuRegions.All)
                {
                    if (possiblePositions.Count == 0 || !possiblePositions.All(region.Contains))
                    {
                        continue;
                    }

                    var eliminations = region.Positions
                        .Where(position => !cagePositions[cage].Contains(position) &&
                                            !state.Board.GetCell(position).CurrentValue.HasValue &&
                                            state.GetCandidates(position).Values.Contains(digit))
                        .Select(position => new CandidateElimination(position, digit))
                        .ToArray();
                    if (eliminations.Length > 0)
                    {
                        steps.Add(new LogicalStep(
                            Id,
                            eliminations: eliminations,
                            relatedPositions: possiblePositions.Concat(eliminations.Select(item => item.Position)),
                            evidence: new LogicalStepEvidence(
                                possiblePositions,
                                region.Positions,
                                [digit],
                                LogicalScopeContext.ForCageRegionIntersection(
                                    GetRegionKind(region.Index),
                                    GetRegionLocalIndex(region.Index),
                                    cage.TargetSum),
                                possiblePositions.Select(position =>
                                    new LogicalPatternCandidate(position, digit)))));
                    }
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }

    private static LogicalScopeKind GetRegionKind(int regionIndex) => regionIndex switch
    {
        < 9 => LogicalScopeKind.Row,
        < 18 => LogicalScopeKind.Column,
        _ => LogicalScopeKind.Block
    };

    private static int GetRegionLocalIndex(int regionIndex) => regionIndex < 9
        ? regionIndex
        : regionIndex < 18
            ? regionIndex - 9
            : regionIndex - 18;
}
