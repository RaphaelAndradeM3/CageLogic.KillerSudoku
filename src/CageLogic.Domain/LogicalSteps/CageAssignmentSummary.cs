using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.LogicalSteps;

internal sealed class CageAssignmentSummary
{
    private readonly Dictionary<CellPosition, HashSet<int>> _valuesByPosition = new();
    private readonly Dictionary<int, HashSet<CellPosition>> _positionsByValue = new();
    private readonly HashSet<int>[] _contributionsByRegion = Enumerable.Range(0, 27)
        .Select(_ => new HashSet<int>()).ToArray();
    private readonly Dictionary<(CellPosition Position, int Value), HashSet<int>>[] _candidateContributionsByRegion =
        Enumerable.Range(0, 27)
            .Select(_ => new Dictionary<(CellPosition Position, int Value), HashSet<int>>()).ToArray();

    private CageAssignmentSummary()
    {
    }

    public IReadOnlyDictionary<CellPosition, HashSet<int>> ValuesByPosition => _valuesByPosition;

    public IReadOnlyDictionary<int, HashSet<CellPosition>> PositionsByValue => _positionsByValue;

    public bool HasAssignments { get; private set; }

    public IReadOnlySet<int> GetContributions(int regionIndex) => _contributionsByRegion[regionIndex];

    public IReadOnlySet<int> GetCandidateContributions(int regionIndex, CellPosition position, int value)
    {
        return _candidateContributionsByRegion[regionIndex].TryGetValue((position, value), out var contributions)
            ? contributions
            : EmptyIntSet;
    }

    public static CageAssignmentSummary Create(LogicalState state, Cage cage, CancellationToken cancellationToken)
    {
        var summary = new CageAssignmentSummary();
        var fixedValues = new Dictionary<CellPosition, int>();
        var usedValues = new HashSet<int>();
        var remaining = new List<CellPosition>();
        var fixedSum = 0;
        foreach (var position in cage.Positions)
        {
            var value = state.Board.GetCell(position).CurrentValue;
            if (value.HasValue)
            {
                if (!usedValues.Add(value.Value))
                {
                    return summary;
                }

                fixedValues.Add(position, value.Value);
                fixedSum += value.Value;
            }
            else
            {
                remaining.Add(position);
                summary._valuesByPosition[position] = new HashSet<int>();
            }
        }

        if (fixedSum > cage.TargetSum)
        {
            return summary;
        }

        remaining = remaining
            .OrderBy(position => state.GetCandidates(position).Values.Count)
            .ThenBy(position => position.Row * 9 + position.Column)
            .ToList();
        var assignment = new Dictionary<CellPosition, int>(fixedValues);
        var visitedNodes = 0;
        Assign(0, fixedSum);
        return summary;

        void Assign(int index, int sum)
        {
            if ((++visitedNodes & 0x3ff) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (index == remaining.Count)
            {
                if (sum == cage.TargetSum)
                {
                    summary.AddAssignment(state, cage, assignment);
                }

                return;
            }

            var position = remaining[index];
            foreach (var value in state.GetCandidates(position).Values.Order())
            {
                if (usedValues.Contains(value) || sum + value > cage.TargetSum)
                {
                    continue;
                }

                usedValues.Add(value);
                assignment[position] = value;
                Assign(index + 1, sum + value);
                assignment.Remove(position);
                usedValues.Remove(value);
            }
        }
    }

    private void AddAssignment(LogicalState state, Cage cage, IReadOnlyDictionary<CellPosition, int> assignment)
    {
        HasAssignments = true;
        foreach (var (position, value) in assignment)
        {
            if (!_valuesByPosition.ContainsKey(position))
            {
                continue;
            }

            _valuesByPosition[position].Add(value);
            if (!_positionsByValue.TryGetValue(value, out var positions))
            {
                positions = new HashSet<CellPosition>();
                _positionsByValue.Add(value, positions);
            }

            positions.Add(position);
        }

        foreach (var region in SudokuRegions.All)
        {
            var contribution = cage.Positions
                .Where(region.Contains)
                .Sum(position => assignment[position]);
            _contributionsByRegion[region.Index].Add(contribution);
            foreach (var position in cage.Positions.Where(region.Contains))
            {
                if (state.Board.GetCell(position).CurrentValue.HasValue)
                {
                    continue;
                }

                var key = (position, assignment[position]);
                if (!_candidateContributionsByRegion[region.Index].TryGetValue(key, out var contributions))
                {
                    contributions = new HashSet<int>();
                    _candidateContributionsByRegion[region.Index].Add(key, contributions);
                }

                contributions.Add(contribution);
            }
        }
    }

    private static IReadOnlySet<int> EmptyIntSet { get; } = new HashSet<int>();
}
