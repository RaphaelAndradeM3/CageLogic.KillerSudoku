using System.Collections.ObjectModel;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Domain.Cages;

/// <summary>A cage as it was supplied to the structural validator.</summary>
public sealed class CageDefinition
{
    public CageDefinition(int targetSum, IEnumerable<PuzzleDefinitionPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        TargetSum = targetSum;
        Positions = new ReadOnlyCollection<PuzzleDefinitionPosition>(positions.ToArray());
    }

    public int TargetSum { get; }

    public IReadOnlyList<PuzzleDefinitionPosition> Positions { get; }
}
