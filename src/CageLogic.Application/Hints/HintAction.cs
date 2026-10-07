using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Hints;

/// <summary>The explicit logical action revealed at hint level 3.</summary>
public abstract record HintAction
{
    private HintAction()
    {
    }

    /// <summary>Places a confirmed value in an empty board position.</summary>
    public sealed record PlaceValue : HintAction
    {
        public PlaceValue(CellPosition position, int value)
        {
            if (value is < 1 or > 9)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "A placed digit must be between 1 and 9.");
            }

            Position = position;
            Value = value;
        }

        public CellPosition Position { get; }

        public int Value { get; }
    }

    /// <summary>Removes the listed candidates in deterministic row-major and digit order.</summary>
    public sealed record RemoveCandidates : HintAction
    {
        public RemoveCandidates(IEnumerable<HintCandidateReference> candidates)
            : base()
        {
            ArgumentNullException.ThrowIfNull(candidates);
            var ordered = candidates
                .Distinct()
                .OrderBy(candidate => candidate.Position.Row * 9 + candidate.Position.Column)
                .ThenBy(candidate => candidate.Value)
                .ToArray();
            if (ordered.Length == 0)
            {
                throw new ArgumentException("At least one candidate must be selected for removal.", nameof(candidates));
            }

            Candidates = new ReadOnlyCollection<HintCandidateReference>(ordered);
        }

        public IReadOnlyList<HintCandidateReference> Candidates { get; }
    }
}
