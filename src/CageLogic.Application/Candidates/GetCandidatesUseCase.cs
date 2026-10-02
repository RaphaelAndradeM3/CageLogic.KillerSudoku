using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;

namespace CageLogic.Application.Candidates;

/// <summary>Returns fresh candidates for the empty positions in the supplied board state.</summary>
public sealed class GetCandidatesUseCase
{
    private readonly CandidateCalculator _calculator = new();

    public IReadOnlyList<CandidateSet> Execute(SudokuBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return _calculator.Calculate(board);
    }
}
