using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionBoardTests
{
	[Test]
	public void CreateFromGeneratedPuzzle_ProjectsAllCellsAndCageTargets()
	{
		var generatedPuzzle = CreateGeneratedPuzzle();
		var session = new GameSession(generatedPuzzle);

		Assert.That(session.ViewState.Cells, Has.Count.EqualTo(81));
		Assert.That(session.ViewState.Cages, Has.Count.EqualTo(81));
		Assert.That(session.ViewState.Cells.All(cell => cell.Value is null), Is.True);
		Assert.That(session.ViewState.Cells.All(cell => !cell.IsGiven && cell.IsEditable), Is.True);
		Assert.That(session.ViewState.Cages[0].TargetSum, Is.EqualTo(generatedPuzzle.Solution.GetValue(new CellPosition(0, 0))));
	}

	[Test]
	public void SelectCell_ChangesSelectionWithoutChangingBoardValues()
	{
		var session = new GameSession(CreateGeneratedPuzzle());
		var selectedPosition = new CellPosition(4, 6);

		session.SelectCell(selectedPosition);

		Assert.That(session.ViewState.SelectedPosition, Is.EqualTo(selectedPosition));
		Assert.That(session.ViewState.Cells.Count(cell => cell.IsSelected), Is.EqualTo(1));
		Assert.That(session.ViewState.Cells.All(cell => cell.Value is null), Is.True);
	}

	[Test]
	public void EnterDigit_WhenItConflictsWithRowValue_ProjectsBothConflictingCells()
	{
		var session = new GameSession(CreateGeneratedPuzzle());
		var firstPosition = new CellPosition(0, 0);
		var secondPosition = new CellPosition(0, 1);

		session.SelectCell(firstPosition);
		session.EnterDigit(1);
		session.SelectCell(secondPosition);
		session.EnterDigit(1);

		Assert.That(session.ViewState.IsBoardValid, Is.False);
		Assert.That(session.ViewState.Cells.Single(cell => cell.Position == firstPosition).HasConflict, Is.True);
		Assert.That(session.ViewState.Cells.Single(cell => cell.Position == secondPosition).HasConflict, Is.True);
	}

	private static GeneratedPuzzle CreateGeneratedPuzzle()
	{
		var values = (from row in Enumerable.Range(0, 9)
					  from column in Enumerable.Range(0, 9)
					  select ((row * 3 + row / 3 + column) % 9) + 1).ToArray();
		var cages = (from row in Enumerable.Range(0, 9)
					 from column in Enumerable.Range(0, 9)
					 let value = values[row * 9 + column]
					 select new CageDefinition(value, [new PuzzleDefinitionPosition(row, column)])).ToArray();
		var validation = new PuzzleStructureValidator().Validate(new PuzzleDefinition(null, cages));
		Assert.That(validation.IsValid, Is.True, string.Join("; ", validation.Issues.Select(issue => issue.Message)));
		var puzzle = validation.Puzzle!;
		var difficulty = new DifficultyAnalysisResult(
			DifficultyAnalysisStatus.Classified,
			DifficultyLevel.Easy,
			catalogVersion: 1,
			Array.Empty<LogicalTechniqueId>());
		return new GeneratedPuzzle(
			puzzle,
			new SolutionGrid(values, puzzle),
			difficulty,
			DifficultyLevel.Easy,
			seed: 1,
			attempts: 1,
			elapsed: TimeSpan.Zero);
	}
}
