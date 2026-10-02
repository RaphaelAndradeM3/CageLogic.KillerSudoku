using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.Puzzles;

/// <summary>Validates raw coordinates and cage definitions before a playable board can be created.</summary>
public sealed class PuzzleStructureValidator
{
    public PuzzleStructureResult Validate(PuzzleDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var issues = new List<PuzzleStructureIssue>();
        var membership = new Dictionary<CellPosition, int>();
        var validatedCages = new List<Cage>();

        for (var cageIndex = 0; cageIndex < definition.Cages.Count; cageIndex++)
        {
            var cage = definition.Cages[cageIndex];
            if (cage.Positions.Count == 0)
            {
                issues.Add(new PuzzleStructureIssue(
                    PuzzleStructureIssueCode.EmptyCage,
                    "A cage must contain at least one position.",
                    Array.Empty<PuzzleDefinitionPosition>(),
                    cageIndex));
                continue;
            }

            var seen = new HashSet<PuzzleDefinitionPosition>();
            var validPositions = new List<CellPosition>();
            var cageHasInvalidCoordinate = false;

            foreach (var rawPosition in cage.Positions)
            {
                if (!seen.Add(rawPosition))
                {
                    issues.Add(new PuzzleStructureIssue(
                        PuzzleStructureIssueCode.DuplicatePositionInCage,
                        "A cage cannot contain the same position more than once.",
                        [rawPosition],
                        cageIndex));
                    continue;
                }

                if (!IsInRange(rawPosition))
                {
                    cageHasInvalidCoordinate = true;
                    issues.Add(new PuzzleStructureIssue(
                        PuzzleStructureIssueCode.OutOfRangePosition,
                        "A position must have a row and column between 0 and 8.",
                        [rawPosition],
                        cageIndex));
                    continue;
                }

                var position = new CellPosition(rawPosition.Row, rawPosition.Column);
                validPositions.Add(position);
                if (membership.TryGetValue(position, out var existingCageIndex))
                {
                    issues.Add(new PuzzleStructureIssue(
                        PuzzleStructureIssueCode.OverlappingPosition,
                        $"Position ({position.Row}, {position.Column}) belongs to cages {existingCageIndex} and {cageIndex}.",
                        [rawPosition],
                        cageIndex));
                }
                else
                {
                    membership.Add(position, cageIndex);
                }
            }

            var uniqueValidPositions = validPositions.Distinct().ToArray();
            if (uniqueValidPositions.Length > 0 && !Cage.AreConnected(uniqueValidPositions))
            {
                issues.Add(new PuzzleStructureIssue(
                    PuzzleStructureIssueCode.DisconnectedCage,
                    "All positions in a cage must be connected by shared sides.",
                    uniqueValidPositions.Select(ToRawPosition),
                    cageIndex));
            }

            if (!cageHasInvalidCoordinate && uniqueValidPositions.Length == cage.Positions.Count &&
                !CageSumFeasibility.CanReachTarget(cage.TargetSum, uniqueValidPositions.Length, Array.Empty<int>()))
            {
                issues.Add(new PuzzleStructureIssue(
                    PuzzleStructureIssueCode.UnreachableTarget,
                    "The cage target cannot be reached using distinct digits from 1 through 9.",
                    cage.Positions,
                    cageIndex));
            }

            if (uniqueValidPositions.Length > 0 && !issues.Any(issue => issue.CageIndex == cageIndex))
            {
                validatedCages.Add(new Cage(cage.TargetSum, uniqueValidPositions));
            }
        }

        for (var row = 0; row < 9; row++)
        {
            for (var column = 0; column < 9; column++)
            {
                var position = new CellPosition(row, column);
                if (!membership.ContainsKey(position))
                {
                    var rawPosition = ToRawPosition(position);
                    issues.Add(new PuzzleStructureIssue(
                        PuzzleStructureIssueCode.UncoveredPosition,
                        $"Position ({row}, {column}) does not belong to a cage.",
                        [rawPosition]));
                }
            }
        }

        var validatedGivens = new Dictionary<CellPosition, int>();
        foreach (var (rawPosition, value) in definition.Givens)
        {
            if (!IsInRange(rawPosition))
            {
                issues.Add(new PuzzleStructureIssue(
                    PuzzleStructureIssueCode.OutOfRangePosition,
                    "A given value must refer to a position with a row and column between 0 and 8.",
                    [rawPosition]));
                continue;
            }

            if (value is < 1 or > 9)
            {
                issues.Add(new PuzzleStructureIssue(
                    PuzzleStructureIssueCode.InvalidGivenValue,
                    "A given value must be between 1 and 9.",
                    [rawPosition]));
                continue;
            }

            validatedGivens.Add(new CellPosition(rawPosition.Row, rawPosition.Column), value);
        }

        if (issues.Count > 0)
        {
            return PuzzleStructureResult.Invalid(issues);
        }

        return PuzzleStructureResult.Valid(new ValidatedPuzzle(validatedGivens, validatedCages));
    }

    private static bool IsInRange(PuzzleDefinitionPosition position)
    {
        return position.Row is >= 0 and <= 8 && position.Column is >= 0 and <= 8;
    }

    private static PuzzleDefinitionPosition ToRawPosition(CellPosition position)
    {
        return new PuzzleDefinitionPosition(position.Row, position.Column);
    }
}
