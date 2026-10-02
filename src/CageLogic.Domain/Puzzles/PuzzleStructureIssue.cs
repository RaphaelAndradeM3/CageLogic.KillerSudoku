using System.Collections.ObjectModel;

namespace CageLogic.Domain.Puzzles;

public enum PuzzleStructureIssueCode
{
    OutOfRangePosition,
    InvalidGivenValue,
    EmptyCage,
    DuplicatePositionInCage,
    UncoveredPosition,
    OverlappingPosition,
    DisconnectedCage,
    UnreachableTarget
}

/// <summary>One structural problem, retaining raw coordinates even when they are out of range.</summary>
public sealed class PuzzleStructureIssue
{
    public PuzzleStructureIssue(
        PuzzleStructureIssueCode code,
        string message,
        IEnumerable<PuzzleDefinitionPosition> positions,
        int? cageIndex = null)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(positions);

        Code = code;
        Message = message;
        Positions = new ReadOnlyCollection<PuzzleDefinitionPosition>(positions.ToArray());
        CageIndex = cageIndex;
    }

    public PuzzleStructureIssueCode Code { get; }

    public string Message { get; }

    public IReadOnlyList<PuzzleDefinitionPosition> Positions { get; }

    public int? CageIndex { get; }
}
