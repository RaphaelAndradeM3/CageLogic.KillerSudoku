using System.Collections.ObjectModel;

namespace CageLogic.Domain.Validation;

/// <summary>Value validation for a board created from a structurally validated puzzle.</summary>
public sealed class BoardValidationResult
{
    internal BoardValidationResult(bool isComplete, IEnumerable<ValueConflict> conflicts)
    {
        IsComplete = isComplete;
        Conflicts = new ReadOnlyCollection<ValueConflict>(conflicts.ToArray());
    }

    public bool IsValid => Conflicts.Count == 0;

    public bool IsComplete { get; }

    public bool IsSolved => IsValid && IsComplete;

    public IReadOnlyList<ValueConflict> Conflicts { get; }
}
