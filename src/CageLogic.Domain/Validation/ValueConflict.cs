using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Domain.Validation;

public enum ValueConflictRule
{
    DuplicateRow,
    DuplicateColumn,
    DuplicateBlock,
    DuplicateCage,
    CageTargetUnreachable
}

/// <summary>A rule violation caused by current board values.</summary>
public sealed class ValueConflict
{
    public ValueConflict(
        ValueConflictRule rule,
        IEnumerable<CellPosition> positions,
        int? repeatedValue = null,
        int? targetSum = null)
    {
        ArgumentNullException.ThrowIfNull(positions);

        Rule = rule;
        Positions = new ReadOnlyCollection<CellPosition>(positions.ToArray());
        RepeatedValue = repeatedValue;
        TargetSum = targetSum;
    }

    public ValueConflictRule Rule { get; }

    public IReadOnlyList<CellPosition> Positions { get; }

    public int? RepeatedValue { get; }

    public int? TargetSum { get; }
}
