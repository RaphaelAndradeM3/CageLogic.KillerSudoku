using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Puzzles;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.Solving;

/// <summary>Counts solutions for a validated Killer Sudoku puzzle, stopping after the second.</summary>
public sealed class SudokuSolver
{
    private readonly CandidateCalculator _candidateCalculator;
    private readonly SudokuBoardValidator _boardValidator;

    public SudokuSolver()
        : this(new CandidateCalculator(), new SudokuBoardValidator())
    {
    }

    public SudokuSolver(CandidateCalculator candidateCalculator, SudokuBoardValidator boardValidator)
    {
        ArgumentNullException.ThrowIfNull(candidateCalculator);
        ArgumentNullException.ThrowIfNull(boardValidator);
        _candidateCalculator = candidateCalculator;
        _boardValidator = boardValidator;
    }

    public SolutionSearchResult FindSolutions(ValidatedPuzzle puzzle, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(puzzle);
        cancellationToken.ThrowIfCancellationRequested();

        SolutionGrid? firstSolution = null;
        var solutionCount = 0;
        var foundSolutions = new HashSet<string>(StringComparer.Ordinal);
        Search(puzzle.CreateBoard());

        return solutionCount switch
        {
            0 => new SolutionSearchResult(SolutionMultiplicity.NoSolution, null, 0),
            1 => new SolutionSearchResult(SolutionMultiplicity.Unique, firstSolution, 1),
            _ => new SolutionSearchResult(SolutionMultiplicity.Multiple, firstSolution, 2)
        };

        void Search(SudokuBoard board)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (solutionCount >= 2)
            {
                return;
            }

            var candidateSets = _candidateCalculator.Calculate(board);
            if (candidateSets.Count == 0)
            {
                if (!_boardValidator.Validate(board).IsSolved)
                {
                    return;
                }

                var values = board.Cells.Select(cell => cell.CurrentValue!.Value).ToArray();
                var key = string.Concat(values.Select(value => (char)('0' + value)));
                if (foundSolutions.Add(key))
                {
                    solutionCount++;
                    firstSolution ??= new SolutionGrid(values, puzzle);
                }

                return;
            }

            var selected = candidateSets
                .OrderBy(candidates => candidates.Values.Count)
                .ThenBy(candidates => candidates.Position.Row * 9 + candidates.Position.Column)
                .First();
            if (selected.Values.Count == 0)
            {
                return;
            }

            foreach (var value in selected.Values.Order())
            {
                cancellationToken.ThrowIfCancellationRequested();
                Search(board.WithPlayerValue(selected.Position, value));
                if (solutionCount >= 2)
                {
                    return;
                }
            }
        }
    }
}
