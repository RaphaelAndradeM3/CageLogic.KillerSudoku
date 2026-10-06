using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

internal static class LogicalStepOrdering
{
    public static LogicalStep? SelectBest(IEnumerable<LogicalStep> steps)
    {
        LogicalStep? best = null;
        foreach (var candidate in steps)
        {
            if (best is null || Compare(candidate, best) < 0)
            {
                best = candidate;
            }
        }

        return best;
    }

    public static int PositionIndex(CellPosition position) => position.Row * 9 + position.Column;

    private static int Compare(LogicalStep left, LogicalStep right)
    {
        var positionsComparison = ComparePositions(left.RelatedPositions, right.RelatedPositions);
        if (positionsComparison != 0)
        {
            return positionsComparison;
        }

        var digitComparison = StepDigit(left).CompareTo(StepDigit(right));
        if (digitComparison != 0)
        {
            return digitComparison;
        }

        var effectComparison = (left.Placement.HasValue ? 0 : 1).CompareTo(right.Placement.HasValue ? 0 : 1);
        if (effectComparison != 0)
        {
            return effectComparison;
        }

        var leftEffects = string.Join(',', left.Eliminations.Select(item => $"{PositionIndex(item.Position):D2}:{item.Value:D1}"));
        var rightEffects = string.Join(',', right.Eliminations.Select(item => $"{PositionIndex(item.Position):D2}:{item.Value:D1}"));
        return string.CompareOrdinal(leftEffects, rightEffects);
    }

    private static int ComparePositions(IReadOnlyList<CellPosition> left, IReadOnlyList<CellPosition> right)
    {
        for (var index = 0; index < Math.Min(left.Count, right.Count); index++)
        {
            var comparison = PositionIndex(left[index]).CompareTo(PositionIndex(right[index]));
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return left.Count.CompareTo(right.Count);
    }

    private static int StepDigit(LogicalStep step)
    {
        return step.Placement?.Value ?? step.Eliminations.Min(elimination => elimination.Value);
    }
}
