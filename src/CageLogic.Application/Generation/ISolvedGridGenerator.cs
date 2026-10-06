namespace CageLogic.Application.Generation;

public interface ISolvedGridGenerator
{
    IReadOnlyList<int> Generate(int seed, CancellationToken cancellationToken = default);
}
