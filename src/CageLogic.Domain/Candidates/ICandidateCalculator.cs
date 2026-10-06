using CageLogic.Domain.Board;

namespace CageLogic.Domain.Candidates;

public interface ICandidateCalculator
{
    IReadOnlyList<CandidateSet> Calculate(SudokuBoard board);
}
