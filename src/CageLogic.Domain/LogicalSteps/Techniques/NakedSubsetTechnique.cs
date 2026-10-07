using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public abstract class NakedSubsetTechnique : ILogicalTechnique
{
    protected NakedSubsetTechnique(LogicalTechniqueId id, int subsetSize)
    {
        Id = id;
        SubsetSize = subsetSize;
    }

    public LogicalTechniqueId Id { get; }

    private int SubsetSize { get; }

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new List<LogicalStep>();
        foreach (var region in SudokuRegions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = region.Positions
                .Where(position => !state.Board.GetCell(position).CurrentValue.HasValue)
                .Select(position => (Position: position, Values: state.GetCandidates(position).Values))
                .Where(cell => cell.Values.Count is > 0 && cell.Values.Count <= SubsetSize)
                .ToArray();
            foreach (var group in Combinations(cells, SubsetSize))
            {
                var digits = group.SelectMany(cell => cell.Values).ToHashSet();
                if (digits.Count != SubsetSize)
                {
                    continue;
                }

                var positions = group.Select(cell => cell.Position).ToHashSet();
                var eliminations = region.Positions
                    .Where(position => !positions.Contains(position) &&
                                       !state.Board.GetCell(position).CurrentValue.HasValue)
                    .SelectMany(position => state.GetCandidates(position).Values
                        .Where(digits.Contains)
                        .Select(digit => new CandidateElimination(position, digit)))
                    .ToArray();
                if (eliminations.Length > 0)
                {
                    steps.Add(new LogicalStep(
                        Id,
                        eliminations: eliminations,
                        relatedPositions: positions.Concat(eliminations.Select(item => item.Position)),
                        evidence: new LogicalStepEvidence(
                            positions,
                            region.Positions,
                            digits,
                            LogicalScopeContext.ForRegion(
                                GetRegionKind(region.Index),
                                GetRegionLocalIndex(region.Index)),
                            group.SelectMany(cell => cell.Values.Select(value =>
                                new LogicalPatternCandidate(cell.Position, value))))));
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }

    private static IEnumerable<T[]> Combinations<T>(IReadOnlyList<T> source, int count)
    {
        var buffer = new T[count];
        return Walk(0, 0);

        IEnumerable<T[]> Walk(int sourceIndex, int bufferIndex)
        {
            if (bufferIndex == count)
            {
                yield return (T[])buffer.Clone();
                yield break;
            }

            for (var index = sourceIndex; index <= source.Count - (count - bufferIndex); index++)
            {
                buffer[bufferIndex] = source[index];
                foreach (var result in Walk(index + 1, bufferIndex + 1))
                {
                    yield return result;
                }
            }
        }
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
