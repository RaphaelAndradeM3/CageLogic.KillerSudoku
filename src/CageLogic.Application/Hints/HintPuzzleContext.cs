using CageLogic.Application.Solving;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Hints;

/// <summary>Immutable original puzzle and its independently confirmed solution multiplicity.</summary>
public sealed class HintPuzzleContext
{
    public HintPuzzleContext(
        ValidatedPuzzle puzzle,
        SolutionMultiplicity multiplicity,
        SolutionGrid? uniqueSolution)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        if (!Enum.IsDefined(multiplicity))
        {
            throw new ArgumentOutOfRangeException(nameof(multiplicity));
        }

        if (multiplicity == SolutionMultiplicity.Unique && uniqueSolution is null)
        {
            throw new ArgumentException("A unique puzzle context requires its confirmed solution.", nameof(uniqueSolution));
        }

        if (multiplicity != SolutionMultiplicity.Unique && uniqueSolution is not null)
        {
            throw new ArgumentException("A solution may be supplied only when puzzle multiplicity is Unique.", nameof(uniqueSolution));
        }

        if (uniqueSolution is not null)
        {
            try
            {
                _ = uniqueSolution.ToBoard(puzzle);
            }
            catch (ArgumentException exception)
            {
                throw new ArgumentException("The confirmed solution does not belong to the supplied puzzle.", nameof(uniqueSolution), exception);
            }
        }

        Puzzle = puzzle;
        Multiplicity = multiplicity;
        UniqueSolution = uniqueSolution;
    }

    public ValidatedPuzzle Puzzle { get; }

    public SolutionMultiplicity Multiplicity { get; }

    public SolutionGrid? UniqueSolution { get; }
}
