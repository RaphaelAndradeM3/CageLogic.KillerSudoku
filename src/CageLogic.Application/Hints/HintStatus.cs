namespace CageLogic.Application.Hints;

/// <summary>Explicit outcome of evaluating a logical hint request.</summary>
public enum HintStatus
{
    Available,
    PuzzleSolved,
    InconsistentState,
    NoSafeHint,
    ValueNotConfirmed
}
