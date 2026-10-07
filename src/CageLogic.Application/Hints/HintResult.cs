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
        string? explanation = null)
    {
        BoardRevision = boardRevision;
        Status = status;
        TechniqueId = techniqueId;
        TechniqueName = techniqueName;
        Explanation = explanation;
    }

    public long BoardRevision { get; }

    public HintStatus Status { get; }

    public LogicalTechniqueId? TechniqueId { get; }

    public string? TechniqueName { get; }

    public string? Explanation { get; }
}
