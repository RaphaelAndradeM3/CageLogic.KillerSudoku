using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class HiddenSingleTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.HiddenSingle;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new List<LogicalStep>();
        foreach (var region in SudokuRegions.All)
        {
            foreach (var digit in Enumerable.Range(1, 9))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var positions = region.Positions
                    .Where(position => !state.Board.GetCell(position).CurrentValue.HasValue &&
                                       state.GetCandidates(position).Values.Contains(digit))
                    .ToArray();
                if (positions.Length == 1)
                {
                    steps.Add(new LogicalStep(
                        Id,
                        new LogicalPlacement(positions[0], digit),
                        evidence: new LogicalStepEvidence(
                            [positions[0]],
                            region.Positions,
                            [digit],
                            CreateRegionContext(region.Index),
                            [new LogicalPatternCandidate(positions[0], digit)])));
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }

    private static LogicalScopeContext CreateRegionContext(int regionIndex)
    {
        var regionKind = regionIndex switch
        {
            < 9 => LogicalScopeKind.Row,
            < 18 => LogicalScopeKind.Column,
            _ => LogicalScopeKind.Block
        };
        var localIndex = regionIndex < 9 ? regionIndex : regionIndex < 18 ? regionIndex - 9 : regionIndex - 18;
        return LogicalScopeContext.ForRegion(regionKind, localIndex);
    }
}
