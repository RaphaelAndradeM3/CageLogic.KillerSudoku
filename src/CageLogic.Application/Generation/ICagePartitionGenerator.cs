using CageLogic.Application.Difficulty;
using CageLogic.Domain.Cages;

namespace CageLogic.Application.Generation;

public interface ICagePartitionGenerator
{
    IReadOnlyList<CageDefinition> Generate(
        IReadOnlyList<int> solution,
        DifficultyLevel difficulty,
        int seed,
        CancellationToken cancellationToken = default);
}
