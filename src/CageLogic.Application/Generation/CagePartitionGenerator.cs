using System.Collections.ObjectModel;
using CageLogic.Application.Difficulty;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Generation;

/// <summary>Partitions a known solved grid into connected cages whose targets match its digits.</summary>
public sealed class CagePartitionGenerator : ICagePartitionGenerator
{
    private static readonly int[] ExpertSingletonIndexes =
    [
        3, 6, 8, 12, 13, 15, 18, 23, 24, 25, 29, 32, 33, 35, 40,
        45, 47, 48, 51, 55, 56, 57, 62, 65, 67, 68, 72, 74, 77
    ];

    public IReadOnlyList<CageDefinition> Generate(
        IReadOnlyList<int> solution,
        DifficultyLevel difficulty,
        int seed,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solution);
        if (solution.Count != 81 || solution.Any(value => value is < 1 or > 9))
        {
            throw new ArgumentException("The solved grid must contain 81 digits from 1 through 9.", nameof(solution));
        }

        if (!Enum.IsDefined(difficulty))
        {
            throw new ArgumentOutOfRangeException(nameof(difficulty));
        }

        if (difficulty == DifficultyLevel.Expert)
        {
            return GenerateExpertPartition(solution, seed, cancellationToken);
        }

        var random = new Random(seed);
        var unassigned = Enumerable.Range(0, 81).ToHashSet();
        var cages = new List<CageDefinition>();
        var maximumSize = difficulty switch
        {
            DifficultyLevel.Easy => 4,
            DifficultyLevel.Medium => 3,
            DifficultyLevel.Hard => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
        var singletonProbability = difficulty switch
        {
            DifficultyLevel.Easy => 0.0,
            DifficultyLevel.Medium => 0.0,
            DifficultyLevel.Hard => 0.20,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };

        var crossBlockProbability = difficulty switch
        {
            DifficultyLevel.Hard => 0.75,
            _ => 0.0
        };

        while (unassigned.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startIndex = unassigned.Order().ElementAt(random.Next(unassigned.Count));
            var chosen = new List<int> { startIndex };
            var usedDigits = new HashSet<int> { solution[startIndex] };
            var targetSize = maximumSize == 1 || random.NextDouble() < singletonProbability
                ? 1
                : random.Next(2, maximumSize + 1);

            while (chosen.Count < targetSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var neighbors = chosen
                    .SelectMany(GetNeighborIndexes)
                    .Where(unassigned.Contains)
                    .Where(index => !usedDigits.Contains(solution[index]))
                    .Distinct()
                    .Order()
                    .ToArray();
                if (neighbors.Length == 0)
                {
                    break;
                }

                if (random.NextDouble() < crossBlockProbability)
                {
                    var startBlock = GetBlockIndex(startIndex);
                    var crossBlockNeighbors = neighbors.Where(index => GetBlockIndex(index) != startBlock).ToArray();
                    if (crossBlockNeighbors.Length > 0)
                    {
                        neighbors = crossBlockNeighbors;
                    }
                }

                var next = neighbors[random.Next(neighbors.Length)];
                chosen.Add(next);
                usedDigits.Add(solution[next]);
            }

            foreach (var index in chosen)
            {
                unassigned.Remove(index);
            }

            var positions = chosen
                .Order()
                .Select(index => new PuzzleDefinitionPosition(index / 9, index % 9))
                .ToArray();
            cages.Add(new CageDefinition(chosen.Sum(index => solution[index]), positions));
        }

        return new ReadOnlyCollection<CageDefinition>(cages);
    }

    private static IReadOnlyList<CageDefinition> GenerateExpertPartition(
        IReadOnlyList<int> solution,
        int seed,
        CancellationToken cancellationToken)
    {
        var indexMap = ExpertPuzzleSymmetry.CreateIndexMap(seed);
        var canonicalSolution = ExpertPuzzleSymmetry.InverseTransformSolution(solution, indexMap);
        var random = new Random(seed);
        var expertSingletons = ExpertSingletonIndexes.ToHashSet();
        var unassigned = Enumerable.Range(0, 81)
            .Where(index => !expertSingletons.Contains(index))
            .ToHashSet();
        var cages = ExpertSingletonIndexes
            .Select(index => new CageDefinition(
                canonicalSolution[index],
                [new PuzzleDefinitionPosition(index / 9, index % 9)]))
            .ToList();

        while (unassigned.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var startIndex = unassigned.Order().ElementAt(random.Next(unassigned.Count));
            var chosen = new List<int> { startIndex };
            var usedDigits = new HashSet<int> { canonicalSolution[startIndex] };
            var targetSize = random.Next(2, 9);

            while (chosen.Count < targetSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var neighbors = chosen
                    .SelectMany(GetNeighborIndexes)
                    .Where(unassigned.Contains)
                    .Where(index => !usedDigits.Contains(canonicalSolution[index]))
                    .Distinct()
                    .Order()
                    .ToArray();
                if (neighbors.Length == 0)
                {
                    break;
                }

                const double crossBlockProbability = 1.0;
                if (random.NextDouble() < crossBlockProbability)
                {
                    var startBlock = GetBlockIndex(startIndex);
                    var crossBlockNeighbors = neighbors.Where(index => GetBlockIndex(index) != startBlock).ToArray();
                    if (crossBlockNeighbors.Length > 0)
                    {
                        neighbors = crossBlockNeighbors;
                    }
                }

                var next = neighbors[random.Next(neighbors.Length)];
                chosen.Add(next);
                usedDigits.Add(canonicalSolution[next]);
            }

            foreach (var index in chosen)
            {
                unassigned.Remove(index);
            }

            var positions = chosen
                .Select(index => new PuzzleDefinitionPosition(index / 9, index % 9))
                .ToArray();
            cages.Add(new CageDefinition(chosen.Sum(index => canonicalSolution[index]), positions));
        }

        return new ReadOnlyCollection<CageDefinition>(ExpertPuzzleSymmetry.TransformCages(cages, indexMap).ToArray());
    }

    private static IEnumerable<int> GetNeighborIndexes(int index)
    {
        var row = index / 9;
        var column = index % 9;
        if (row > 0)
        {
            yield return index - 9;
        }

        if (row < 8)
        {
            yield return index + 9;
        }

        if (column > 0)
        {
            yield return index - 1;
        }

        if (column < 8)
        {
            yield return index + 1;
        }
    }

    private static int GetBlockIndex(int index)
    {
        var row = index / 9;
        var column = index % 9;
        return (row / 3 * 3) + column / 3;
    }
}
