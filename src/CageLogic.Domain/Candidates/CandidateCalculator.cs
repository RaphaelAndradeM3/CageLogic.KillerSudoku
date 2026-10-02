using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.Candidates;

/// <summary>Calculates candidates from current row, column, block, and cage restrictions only.</summary>
public sealed class CandidateCalculator
{
    public IReadOnlyList<CandidateSet> Calculate(SudokuBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var results = new List<CandidateSet>();
        foreach (var cell in board.Cells.Where(cell => !cell.CurrentValue.HasValue))
        {
            results.Add(CalculateForEmptyCell(board, cell.Position));
        }

        return Array.AsReadOnly(results.ToArray());
    }

    private static CandidateSet CalculateForEmptyCell(SudokuBoard board, CellPosition position)
    {
        var forbiddenDigits = board.Cells
            .Where(cell => cell.CurrentValue.HasValue &&
                           (cell.Position.Row == position.Row ||
                            cell.Position.Column == position.Column ||
                            IsInSameBlock(cell.Position, position)))
            .Select(cell => cell.CurrentValue!.Value)
            .ToHashSet();

        var cage = board.GetCage(position);
        var cageCells = cage.Positions.Select(board.GetCell).ToArray();
        var usedCageDigits = cageCells
            .Where(cell => cell.CurrentValue.HasValue)
            .Select(cell => cell.CurrentValue!.Value)
            .ToArray();
        var emptyCellsRemainingAfterCandidate = cageCells.Count(cell => !cell.CurrentValue.HasValue) - 1;
        var candidates = new List<int>();

        for (var digit = 1; digit <= 9; digit++)
        {
            if (forbiddenDigits.Contains(digit) || usedCageDigits.Contains(digit))
            {
                continue;
            }

            if (CageSumFeasibility.CanReachTarget(
                    cage.TargetSum,
                    emptyCellsRemainingAfterCandidate,
                    usedCageDigits.Append(digit)))
            {
                candidates.Add(digit);
            }
        }

        return new CandidateSet(position, candidates);
    }

    private static bool IsInSameBlock(CellPosition left, CellPosition right)
    {
        return left.Row / 3 == right.Row / 3 && left.Column / 3 == right.Column / 3;
    }
}
