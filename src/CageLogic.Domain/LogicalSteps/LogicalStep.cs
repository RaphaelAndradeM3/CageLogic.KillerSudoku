using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

/// <summary>A deterministic, presentation-neutral placement or candidate-elimination deduction.</summary>
public sealed class LogicalStep
{
    public LogicalStep(
        LogicalTechniqueId techniqueId,
        LogicalPlacement? placement = null,
        IEnumerable<CandidateElimination>? eliminations = null,
        IEnumerable<CellPosition>? relatedPositions = null)
    {
        if (!Enum.IsDefined(techniqueId))
        {
            throw new ArgumentOutOfRangeException(nameof(techniqueId));
        }

        var eliminationArray = (eliminations ?? Array.Empty<CandidateElimination>())
            .Distinct()
            .OrderBy(elimination => ToIndex(elimination.Position))
            .ThenBy(elimination => elimination.Value)
            .ToArray();
        if (placement.HasValue == (eliminationArray.Length > 0))
        {
            throw new ArgumentException("A logical step must contain either one placement or one or more eliminations.");
        }

        TechniqueId = techniqueId;
        Placement = placement;
        Eliminations = new ReadOnlyCollection<CandidateElimination>(eliminationArray);
        RelatedPositions = new ReadOnlyCollection<CellPosition>(
            (relatedPositions ?? Array.Empty<CellPosition>())
            .Concat(placement.HasValue ? [placement.Value.Position] : Array.Empty<CellPosition>())
            .Concat(eliminationArray.Select(elimination => elimination.Position))
            .Distinct()
            .OrderBy(ToIndex)
            .ToArray());
    }

    public LogicalTechniqueId TechniqueId { get; }

    public LogicalPlacement? Placement { get; }

    public IReadOnlyList<CandidateElimination> Eliminations { get; }

    public IReadOnlyList<CellPosition> RelatedPositions { get; }

    private static int ToIndex(CellPosition position) => position.Row * 9 + position.Column;
}
