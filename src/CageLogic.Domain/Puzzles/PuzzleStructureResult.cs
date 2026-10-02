using System.Collections.ObjectModel;

namespace CageLogic.Domain.Puzzles;

/// <summary>The discriminated result of validating a raw puzzle structure.</summary>
public sealed class PuzzleStructureResult
{
    private PuzzleStructureResult(ValidatedPuzzle? puzzle, IEnumerable<PuzzleStructureIssue> issues)
    {
        Puzzle = puzzle;
        Issues = new ReadOnlyCollection<PuzzleStructureIssue>(issues.ToArray());
    }

    public bool IsValid => Puzzle is not null && Issues.Count == 0;

    public ValidatedPuzzle? Puzzle { get; }

    public IReadOnlyList<PuzzleStructureIssue> Issues { get; }

    public static PuzzleStructureResult Valid(ValidatedPuzzle puzzle)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        return new PuzzleStructureResult(puzzle, Array.Empty<PuzzleStructureIssue>());
    }

    public static PuzzleStructureResult Invalid(IEnumerable<PuzzleStructureIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        var issueArray = issues.ToArray();
        if (issueArray.Length == 0)
        {
            throw new ArgumentException("Invalid structure must include at least one issue.", nameof(issues));
        }

        return new PuzzleStructureResult(null, issueArray);
    }
}
