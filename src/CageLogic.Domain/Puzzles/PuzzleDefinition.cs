using System.Collections.ObjectModel;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.Puzzles;

/// <summary>A raw puzzle definition; malformed coordinates and cages remain reportable.</summary>
public sealed class PuzzleDefinition
{
    public PuzzleDefinition(
        IReadOnlyDictionary<PuzzleDefinitionPosition, int>? givens,
        IEnumerable<CageDefinition> cages)
    {
        ArgumentNullException.ThrowIfNull(cages);

        Givens = new ReadOnlyDictionary<PuzzleDefinitionPosition, int>(
            givens is null
                ? new Dictionary<PuzzleDefinitionPosition, int>()
                : new Dictionary<PuzzleDefinitionPosition, int>(givens));
        Cages = new ReadOnlyCollection<CageDefinition>(cages.ToArray());
    }

    public IReadOnlyDictionary<PuzzleDefinitionPosition, int> Givens { get; }

    public IReadOnlyList<CageDefinition> Cages { get; }
}
