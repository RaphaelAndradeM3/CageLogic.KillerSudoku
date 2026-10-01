using System.Collections.ObjectModel;
using CageLogic.Domain.Board;

namespace CageLogic.Domain.Cages;

/// <summary>A structurally validated, connected group of cells with an exact sum target.</summary>
public sealed class Cage
{
    public Cage(int targetSum, IEnumerable<CellPosition> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var positionArray = positions.ToArray();

        if (positionArray.Length == 0)
        {
            throw new ArgumentException("A cage must contain at least one position.", nameof(positions));
        }

        if (positionArray.Distinct().Count() != positionArray.Length)
        {
            throw new ArgumentException("A cage cannot contain a position more than once.", nameof(positions));
        }

        if (!AreConnected(positionArray))
        {
            throw new ArgumentException("All positions in a cage must be connected by sides.", nameof(positions));
        }

        if (!CageSumFeasibility.CanReachTarget(targetSum, positionArray.Length, Array.Empty<int>()))
        {
            throw new ArgumentOutOfRangeException(nameof(targetSum), targetSum, "The target cannot be reached with distinct digits 1 through 9.");
        }

        TargetSum = targetSum;
        Positions = new ReadOnlyCollection<CellPosition>(positionArray);
    }

    public int TargetSum { get; }

    public IReadOnlyList<CellPosition> Positions { get; }

    internal static bool AreConnected(IReadOnlyCollection<CellPosition> positions)
    {
        var all = positions.ToHashSet();
        var visited = new HashSet<CellPosition>();
        var pending = new Queue<CellPosition>();
        pending.Enqueue(positions.First());
        visited.Add(positions.First());

        while (pending.TryDequeue(out var current))
        {
            foreach (var neighbor in GetOrthogonalNeighbors(current))
            {
                if (all.Contains(neighbor) && visited.Add(neighbor))
                {
                    pending.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == all.Count;
    }

    private static IEnumerable<CellPosition> GetOrthogonalNeighbors(CellPosition position)
    {
        if (position.Row > 0)
        {
            yield return new CellPosition(position.Row - 1, position.Column);
        }

        if (position.Row < 8)
        {
            yield return new CellPosition(position.Row + 1, position.Column);
        }

        if (position.Column > 0)
        {
            yield return new CellPosition(position.Row, position.Column - 1);
        }

        if (position.Column < 8)
        {
            yield return new CellPosition(position.Row, position.Column + 1);
        }
    }
}
