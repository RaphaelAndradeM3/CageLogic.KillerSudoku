using System.Collections.ObjectModel;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;

namespace CageLogic.Domain.Puzzles;

/// <summary>A puzzle whose givens and complete cage partition passed structural validation.</summary>
public sealed class ValidatedPuzzle
{
    internal ValidatedPuzzle(IReadOnlyDictionary<CellPosition, int> givens, IEnumerable<Cage> cages)
    {
        Givens = new ReadOnlyDictionary<CellPosition, int>(new Dictionary<CellPosition, int>(givens));
        Cages = new ReadOnlyCollection<Cage>(cages.ToArray());
    }

    public IReadOnlyDictionary<CellPosition, int> Givens { get; }

    public IReadOnlyList<Cage> Cages { get; }

    public SudokuBoard CreateBoard()
    {
        return SudokuBoard.FromPuzzle(this);
    }
}
