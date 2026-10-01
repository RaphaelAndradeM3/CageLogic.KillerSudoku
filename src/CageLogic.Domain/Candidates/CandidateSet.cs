using System.Collections.Frozen;
using CageLogic.Domain.Board;

namespace CageLogic.Domain.Candidates;

/// <summary>The immutable candidate digits for one empty board position.</summary>
public sealed class CandidateSet
{
    public CandidateSet(CellPosition position, IEnumerable<int> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        var valueSet = values.ToHashSet();
        if (valueSet.Any(value => value is < 1 or > 9))
        {
            throw new ArgumentOutOfRangeException(nameof(values), "Candidates must be digits from 1 through 9.");
        }

        Position = position;
        Values = valueSet.ToFrozenSet();
    }

    public CellPosition Position { get; }

    public IReadOnlySet<int> Values { get; }
}
