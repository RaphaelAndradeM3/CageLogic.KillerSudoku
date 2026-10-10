using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Application.Solving;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Tests.GameSessions;

internal static class GameSessionTestData
{
	public static GeneratedPuzzle CreateGeneratedPuzzle(bool oneCagePerRow = false, bool includeInitialGivens = false)
	{
		var values = (from row in Enumerable.Range(0, 9)
					  from column in Enumerable.Range(0, 9)
					  select ExpectedValue(row, column)).ToArray();
		var cages = oneCagePerRow
			? Enumerable.Range(0, 9).Select(row => new CageDefinition(
				45,
				Enumerable.Range(0, 9).Select(column => new PuzzleDefinitionPosition(row, column))))
			.ToArray()
			: (from row in Enumerable.Range(0, 9)
			   from column in Enumerable.Range(0, 9)
			   select new CageDefinition(values[row * 9 + column], [new PuzzleDefinitionPosition(row, column)])).ToArray();
		var givens = includeInitialGivens
			? Enumerable.Range(0, 27).ToDictionary(
				index => new PuzzleDefinitionPosition(index / 9, index % 9),
				index => values[index])
			: null;
		var validation = new PuzzleStructureValidator().Validate(new PuzzleDefinition(givens, cages));
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
			seed: 73,
			attempts: 1,
			elapsed: TimeSpan.Zero);
	}

	public static int ExpectedValue(int row, int column) => ((row * 3 + row / 3 + column) % 9) + 1;
}
