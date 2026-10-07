using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

/// <summary>Describes a logical pattern and its scope without presentation text.</summary>
public sealed class LogicalStepEvidence
{
    public LogicalStepEvidence(
        IEnumerable<CellPosition> patternPositions,
        IEnumerable<CellPosition>? scopePositions = null,
        IEnumerable<int>? relevantDigits = null,
        LogicalScopeContext? scopeContext = null)
    {
        ArgumentNullException.ThrowIfNull(patternPositions);

        var pattern = NormalizePositions(patternPositions);
        if (pattern.Length == 0)
        {
            throw new ArgumentException("Logical evidence must identify at least one pattern position.", nameof(patternPositions));
        }

        var scope = NormalizePositions(scopePositions ?? Array.Empty<CellPosition>());
        var digits = (relevantDigits ?? Array.Empty<int>()).ToArray();
        if (digits.Any(digit => digit is < 1 or > 9))
        {
            throw new ArgumentOutOfRangeException(nameof(relevantDigits), "Relevant digits must be between 1 and 9.");
        }

        PatternPositions = Array.AsReadOnly(pattern);
        ScopePositions = Array.AsReadOnly(scope);
        RelevantDigits = Array.AsReadOnly(digits.Distinct().Order().ToArray());
        ScopeContext = scopeContext;
    }

    public IReadOnlyList<CellPosition> PatternPositions { get; }

    public IReadOnlyList<CellPosition> ScopePositions { get; }

    public IReadOnlyList<int> RelevantDigits { get; }

    public LogicalScopeContext? ScopeContext { get; }

    private static CellPosition[] NormalizePositions(IEnumerable<CellPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        return positions
            .Distinct()
            .OrderBy(position => position.Row * 9 + position.Column)
            .ToArray();
    }
}

/// <summary>Optional typed context for a logical region, cage, intersection, or Rule of 45.</summary>
public sealed class LogicalScopeContext
{
    private LogicalScopeContext(
        LogicalScopeKind kind,
        LogicalScopeKind? regionKind = null,
        int? regionIndex = null,
        int? targetSum = null,
        int? residualSum = null,
        IEnumerable<int>? relatedCageTargetSums = null)
    {
        Kind = kind;
        RegionKind = regionKind;
        RegionIndex = regionIndex;
        TargetSum = targetSum;
        ResidualSum = residualSum;
        var targetSums = (relatedCageTargetSums ?? Array.Empty<int>()).ToArray();
        if (targetSums.Any(sum => sum is < 1 or > 45))
        {
            throw new ArgumentOutOfRangeException(nameof(relatedCageTargetSums));
        }

        RelatedCageTargetSums = Array.AsReadOnly(targetSums);
    }

    public LogicalScopeKind Kind { get; }

    /// <summary>The row, column, or block containing a region-based deduction.</summary>
    public LogicalScopeKind? RegionKind { get; }

    /// <summary>Zero-based index within the region kind, or the 0–26 Rule of 45 region index.</summary>
    public int? RegionIndex { get; }

    public int? TargetSum { get; }

    public int? ResidualSum { get; }

    public IReadOnlyList<int> RelatedCageTargetSums { get; }

    public static LogicalScopeContext ForRegion(LogicalScopeKind regionKind, int index)
    {
        ValidateRegion(regionKind, index);
        return new LogicalScopeContext(regionKind, regionKind, index);
    }

    public static LogicalScopeContext ForCage(int targetSum)
    {
        ValidateTargetSum(targetSum);
        return new LogicalScopeContext(LogicalScopeKind.Cage, targetSum: targetSum);
    }

    public static LogicalScopeContext ForCageRegionIntersection(
        LogicalScopeKind regionKind,
        int regionIndex,
        int cageTargetSum)
    {
        ValidateRegion(regionKind, regionIndex);
        ValidateTargetSum(cageTargetSum);
        return new LogicalScopeContext(
            LogicalScopeKind.CageRegionIntersection,
            regionKind,
            regionIndex,
            targetSum: cageTargetSum);
    }

    public static LogicalScopeContext ForRuleOf45(
        LogicalScopeKind regionKind,
        int regionIndex,
        int residualSum,
        IEnumerable<int> relatedCageTargetSums)
    {
        ValidateRegion(regionKind, regionIndex);
        ArgumentNullException.ThrowIfNull(relatedCageTargetSums);
        if (residualSum is < 0 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(residualSum));
        }

        return new LogicalScopeContext(
            LogicalScopeKind.RuleOf45,
            regionKind,
            regionIndex,
            residualSum: residualSum,
            relatedCageTargetSums: relatedCageTargetSums);
    }

    private static void ValidateRegion(LogicalScopeKind regionKind, int index)
    {
        if (regionKind is not (LogicalScopeKind.Row or LogicalScopeKind.Column or LogicalScopeKind.Block))
        {
            throw new ArgumentOutOfRangeException(nameof(regionKind));
        }

        if (index is < 0 or > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private static void ValidateTargetSum(int targetSum)
    {
        if (targetSum is < 1 or > 45)
        {
            throw new ArgumentOutOfRangeException(nameof(targetSum));
        }
    }
}

public enum LogicalScopeKind
{
    Row,
    Column,
    Block,
    Cage,
    CageRegionIntersection,
    RuleOf45
}
