using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.Validation;

/// <summary>Reports all current value conflicts without mutating the board.</summary>
public sealed class SudokuBoardValidator
{
    public BoardValidationResult Validate(SudokuBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var conflicts = new List<ValueConflict>();

        for (var index = 0; index < 9; index++)
        {
            AddRepeatedValues(
                board.Cells.Where(cell => cell.Position.Row == index),
                ValueConflictRule.DuplicateRow,
                conflicts);
            AddRepeatedValues(
                board.Cells.Where(cell => cell.Position.Column == index),
                ValueConflictRule.DuplicateColumn,
                conflicts);
        }

        for (var blockRow = 0; blockRow < 3; blockRow++)
        {
            for (var blockColumn = 0; blockColumn < 3; blockColumn++)
            {
                var rowStart = blockRow * 3;
                var columnStart = blockColumn * 3;
                AddRepeatedValues(
                    board.Cells.Where(cell =>
                        cell.Position.Row >= rowStart && cell.Position.Row < rowStart + 3 &&
                        cell.Position.Column >= columnStart && cell.Position.Column < columnStart + 3),
                    ValueConflictRule.DuplicateBlock,
                    conflicts);
            }
        }

        foreach (var cage in board.Cages)
        {
            var cells = cage.Positions.Select(board.GetCell).ToArray();
            AddRepeatedValues(cells, ValueConflictRule.DuplicateCage, conflicts);

            var values = cells.Where(cell => cell.CurrentValue.HasValue).Select(cell => cell.CurrentValue!.Value).ToArray();
            var emptyCount = cells.Count(cell => !cell.CurrentValue.HasValue);
            if (!CageSumFeasibility.CanReachTarget(cage.TargetSum, emptyCount, values))
            {
                conflicts.Add(new ValueConflict(
                    ValueConflictRule.CageTargetUnreachable,
                    cage.Positions,
                    targetSum: cage.TargetSum));
            }
        }

        return new BoardValidationResult(
            board.Cells.All(cell => cell.CurrentValue.HasValue),
            conflicts);
    }

    private static void AddRepeatedValues(
        IEnumerable<SudokuCell> cells,
        ValueConflictRule rule,
        ICollection<ValueConflict> conflicts)
    {
        foreach (var repeated in cells
                     .Where(cell => cell.CurrentValue.HasValue)
                     .GroupBy(cell => cell.CurrentValue!.Value)
                     .Where(group => group.Count() > 1))
        {
            conflicts.Add(new ValueConflict(
                rule,
                repeated.Select(cell => cell.Position),
                repeatedValue: repeated.Key));
        }
    }
}
