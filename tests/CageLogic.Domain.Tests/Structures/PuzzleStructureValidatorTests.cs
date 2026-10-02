using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Tests.Support;

namespace CageLogic.Domain.Tests.Structures;

public sealed class PuzzleStructureValidatorTests
{
    private readonly PuzzleStructureValidator _validator = new();

    [Test]
    public void Validate_AllCellsCoveredByValidCages_ReturnsValidatedPuzzle()
    {
        var result = _validator.Validate(PuzzleTestData.CreateDefinition());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Issues, Is.Empty);
        Assert.That(result.Puzzle, Is.Not.Null);
        Assert.That(result.Puzzle!.Cages, Has.Count.EqualTo(81));
    }

    [Test]
    public void Validate_OutOfRangePosition_ReportsRawPositionAsStructureIssue()
    {
        var cages = PuzzleTestData.CreateSingletonCages()
            .Where(cage => cage.Positions[0] != new PuzzleDefinitionPosition(0, 0))
            .Append(new CageDefinition(1, [new PuzzleDefinitionPosition(9, 0)]));

        var result = _validator.Validate(PuzzleTestData.CreateDefinition(cages));

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Puzzle, Is.Null);
        Assert.That(result.Issues.Any(issue =>
            issue.Code == PuzzleStructureIssueCode.OutOfRangePosition &&
            issue.Positions.Contains(new PuzzleDefinitionPosition(9, 0))), Is.True);
    }

    [Test]
    public void Validate_EmptyCage_ReportsStructureIssue()
    {
        var cages = PuzzleTestData.CreateSingletonCages()
            .Append(new CageDefinition(0, Array.Empty<PuzzleDefinitionPosition>()));

        var result = _validator.Validate(PuzzleTestData.CreateDefinition(cages));

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.EmptyCage), Is.True);
    }

    [Test]
    public void Validate_RepeatedPositionWithinCage_ReportsDuplicate()
    {
        var cages = PuzzleTestData.CreateSingletonCages()
            .Where(cage => cage.Positions[0] != new PuzzleDefinitionPosition(0, 0))
            .Append(new CageDefinition(1,
            [
                new PuzzleDefinitionPosition(0, 0),
                new PuzzleDefinitionPosition(0, 0)
            ]));

        var result = _validator.Validate(PuzzleTestData.CreateDefinition(cages));

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.DuplicatePositionInCage), Is.True);
    }

    [Test]
    public void Validate_UncoveredCell_ReportsMissingPosition()
    {
        var cages = PuzzleTestData.CreateSingletonCages()
            .Where(cage => cage.Positions[0] != new PuzzleDefinitionPosition(0, 0));

        var result = _validator.Validate(PuzzleTestData.CreateDefinition(cages));

        Assert.That(result.Issues.Any(issue =>
            issue.Code == PuzzleStructureIssueCode.UncoveredPosition &&
            issue.Positions.Contains(new PuzzleDefinitionPosition(0, 0))), Is.True);
    }

    [Test]
    public void Validate_PositionInMoreThanOneCage_ReportsOverlap()
    {
        var cages = PuzzleTestData.CreateSingletonCages()
            .Append(new CageDefinition(1, [new PuzzleDefinitionPosition(0, 0)]));

        var result = _validator.Validate(PuzzleTestData.CreateDefinition(cages));

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.OverlappingPosition), Is.True);
    }

    [Test]
    public void Validate_DiagonalOnlyCage_IsDisconnected()
    {
        var puzzle = PuzzleTestData.WithCage([(0, 0), (1, 1)], targetSum: 3);

        var result = _validator.Validate(puzzle);

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.DisconnectedCage), Is.True);
    }

    [Test]
    public void Validate_TargetUnavailableToDistinctDigits_ReportsUnreachableTarget()
    {
        var puzzle = PuzzleTestData.WithCage([(0, 0)], targetSum: 10);

        var result = _validator.Validate(puzzle);

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.UnreachableTarget), Is.True);
    }

    [Test]
    public void Validate_GivenValueOutsideOneToNine_ReportsStructureIssue()
    {
        var puzzle = PuzzleTestData.CreateDefinition(givens: PuzzleTestData.Givens((0, 0, 0)));

        var result = _validator.Validate(puzzle);

        Assert.That(result.Issues.Any(issue => issue.Code == PuzzleStructureIssueCode.InvalidGivenValue), Is.True);
    }
}
