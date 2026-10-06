using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps;

internal sealed class SudokuRegion
{
    private readonly HashSet<CellPosition> _positions;

    public SudokuRegion(int index, IEnumerable<CellPosition> positions)
    {
        Index = index;
        Positions = positions.OrderBy(position => position.Row * 9 + position.Column).ToArray();
        _positions = Positions.ToHashSet();
    }

    public int Index { get; }

    public IReadOnlyList<CellPosition> Positions { get; }

    public bool Contains(CellPosition position) => _positions.Contains(position);
}

internal static class SudokuRegions
{
    public static IReadOnlyList<SudokuRegion> All { get; } = CreateAll();

    private static SudokuRegion[] CreateAll()
    {
        var regions = new List<SudokuRegion>(27);
        for (var row = 0; row < 9; row++)
        {
            var currentRow = row;
            regions.Add(new SudokuRegion(regions.Count,
                Enumerable.Range(0, 9).Select(column => new CellPosition(currentRow, column))));
        }

        for (var column = 0; column < 9; column++)
        {
            var currentColumn = column;
            regions.Add(new SudokuRegion(regions.Count,
                Enumerable.Range(0, 9).Select(row => new CellPosition(row, currentColumn))));
        }

        for (var blockRow = 0; blockRow < 3; blockRow++)
        {
            for (var blockColumn = 0; blockColumn < 3; blockColumn++)
            {
                var currentBlockRow = blockRow;
                var currentBlockColumn = blockColumn;
                regions.Add(new SudokuRegion(regions.Count,
                    from rowOffset in Enumerable.Range(0, 3)
                    from columnOffset in Enumerable.Range(0, 3)
                    select new CellPosition(currentBlockRow * 3 + rowOffset, currentBlockColumn * 3 + columnOffset)));
            }
        }

        return regions.ToArray();
    }
}
