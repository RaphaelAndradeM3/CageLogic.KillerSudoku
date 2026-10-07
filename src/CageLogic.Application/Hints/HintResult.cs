using System.Collections.ObjectModel;
using CageLogic.Domain.Board;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Hints;

/// <summary>Immutable result for the status and explanatory part of a hint request.</summary>
public sealed class HintResult
{
    internal HintResult(
        long boardRevision,
        HintStatus status,
        LogicalTechniqueId? techniqueId = null,
        string? techniqueName = null,
        string? explanation = null,
        IReadOnlyDictionary<HintHighlightRole, IReadOnlyList<CellPosition>>? highlights = null,
        IEnumerable<HintCandidateReference>? involvedCandidates = null,
        HintAction? action = null)
    {
        BoardRevision = boardRevision;
        Status = status;
        TechniqueId = techniqueId;
        TechniqueName = techniqueName;
        Explanation = explanation;
        Highlights = NormalizeHighlights(highlights);
        InvolvedCandidates = Array.AsReadOnly((involvedCandidates ?? Array.Empty<HintCandidateReference>())
            .Distinct()
            .OrderBy(candidate => candidate.Position.Row * 9 + candidate.Position.Column)
            .ThenBy(candidate => candidate.Value)
            .ToArray());
        Action = action;
    }

    public long BoardRevision { get; }

    public HintStatus Status { get; }

    public LogicalTechniqueId? TechniqueId { get; }

    public string? TechniqueName { get; }

    public string? Explanation { get; }

    /// <summary>Cell positions grouped by semantic highlight role; empty before level 2.</summary>
    public IReadOnlyDictionary<HintHighlightRole, IReadOnlyList<CellPosition>> Highlights { get; }

    /// <summary>Candidate position/digit pairs involved in the step, without an action label before level 3.</summary>
    public IReadOnlyList<HintCandidateReference> InvolvedCandidates { get; }

    /// <summary>The explicit placement or removal action, available only at level 3.</summary>
    public HintAction? Action { get; }

    private static IReadOnlyDictionary<HintHighlightRole, IReadOnlyList<CellPosition>> NormalizeHighlights(
        IReadOnlyDictionary<HintHighlightRole, IReadOnlyList<CellPosition>>? highlights)
    {
        var normalized = new Dictionary<HintHighlightRole, IReadOnlyList<CellPosition>>();
        if (highlights is not null)
        {
            foreach (var (role, positions) in highlights)
            {
                if (!Enum.IsDefined(role))
                {
                    throw new ArgumentOutOfRangeException(nameof(highlights), role, "Unknown hint highlight role.");
                }

                ArgumentNullException.ThrowIfNull(positions);
                var ordered = positions
                    .Distinct()
                    .OrderBy(position => position.Row * 9 + position.Column)
                    .ToArray();
                if (ordered.Length > 0)
                {
                    normalized.Add(role, Array.AsReadOnly(ordered));
                }
            }
        }

        return new ReadOnlyDictionary<HintHighlightRole, IReadOnlyList<CellPosition>>(normalized);
    }
}
