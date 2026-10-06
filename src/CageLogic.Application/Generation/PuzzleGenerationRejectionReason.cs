namespace CageLogic.Application.Generation;

public enum PuzzleGenerationRejectionReason
{
    InvalidStructure,
    NoSolution,
    MultipleSolutions,
    GeneratedSolutionMismatch,
    Unclassifiable,
    DifficultyMismatch
}
