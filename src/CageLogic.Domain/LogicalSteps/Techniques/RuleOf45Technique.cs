using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class RuleOf45Technique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.RuleOf45;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new List<LogicalStep>();
        foreach (var region in SudokuRegions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var containedCages = state.Board.Cages
                .Where(cage => cage.Positions.All(region.Contains))
                .ToArray();
            var crossingCages = state.Board.Cages
                .Where(cage => cage.Positions.Any(region.Contains) && !cage.Positions.All(region.Contains))
                .OrderBy(cage => cage.Positions.Min(LogicalStepOrdering.PositionIndex))
                .ToArray();
            if (crossingCages.Length == 0)
            {
                continue;
            }

            var residual = 45 - containedCages.Sum(cage => cage.TargetSum);
            if (residual is < 0 or > 45)
            {
                continue;
            }

            var summaries = crossingCages
                .Select(cage => state.GetCageAssignments(cage, cancellationToken))
                .ToArray();
            if (summaries.Any(summary => !summary.HasAssignments))
            {
                continue;
            }

            var prefix = new HashSet<int>[crossingCages.Length + 1];
            var suffix = new HashSet<int>[crossingCages.Length + 1];
            prefix[0] = [0];
            for (var index = 0; index < crossingCages.Length; index++)
            {
                prefix[index + 1] = AddSums(prefix[index], summaries[index].GetContributions(region.Index));
            }

            suffix[crossingCages.Length] = [0];
            for (var index = crossingCages.Length - 1; index >= 0; index--)
            {
                suffix[index] = AddSums(summaries[index].GetContributions(region.Index), suffix[index + 1]);
            }

            if (!prefix[crossingCages.Length].Contains(residual))
            {
                continue;
            }

            var eliminations = new List<CandidateElimination>();
            for (var cageIndex = 0; cageIndex < crossingCages.Length; cageIndex++)
            {
                var cage = crossingCages[cageIndex];
                foreach (var position in cage.Positions.Where(region.Contains)
                             .Where(position => !state.Board.GetCell(position).CurrentValue.HasValue)
                             .OrderBy(LogicalStepOrdering.PositionIndex))
                {
                    foreach (var digit in state.GetCandidates(position).Values.Order())
                    {
                        var contributions = summaries[cageIndex]
                            .GetCandidateContributions(region.Index, position, digit);
                        if (!contributions.Any(contribution => IsSupported(
                                residual - contribution,
                                prefix[cageIndex],
                                suffix[cageIndex + 1])))
                        {
                            eliminations.Add(new CandidateElimination(position, digit));
                        }
                    }
                }
            }

            if (eliminations.Count > 0)
            {
                steps.Add(new LogicalStep(Id, eliminations: eliminations, relatedPositions: region.Positions));
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }

    private static HashSet<int> AddSums(IEnumerable<int> left, IEnumerable<int> right)
    {
        var sums = new HashSet<int>();
        foreach (var leftSum in left)
        {
            foreach (var rightSum in right)
            {
                var sum = leftSum + rightSum;
                if (sum <= 45)
                {
                    sums.Add(sum);
                }
            }
        }

        return sums;
    }

    private static bool IsSupported(int residual, IReadOnlySet<int> prefix, IReadOnlySet<int> suffix)
    {
        foreach (var prefixSum in prefix)
        {
            if (suffix.Contains(residual - prefixSum))
            {
                return true;
            }
        }

        return false;
    }
}
