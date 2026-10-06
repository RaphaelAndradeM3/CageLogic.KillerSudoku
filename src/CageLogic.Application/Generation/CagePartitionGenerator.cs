using System.Collections.ObjectModel;
using CageLogic.Application.Difficulty;
using CageLogic.Domain.Cages;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.Generation;

/// <summary>Partitions a known solved grid into connected cages whose targets match its digits.</summary>
public sealed class CagePartitionGenerator : ICagePartitionGenerator
{
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

        var random = new Random(seed);
        var unassigned = Enumerable.Range(0, 81).ToHashSet();
        var cages = new List<CageDefinition>();
        var maximumSize = difficulty switch
        {
            DifficultyLevel.Easy => 1,
            DifficultyLevel.Medium => 3,
            DifficultyLevel.Hard => 4,
            DifficultyLevel.Expert => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
        var singletonProbability = difficulty switch
        {
            DifficultyLevel.Easy => 1.0,
            DifficultyLevel.Medium => 0.0,
            DifficultyLevel.Hard => 0.20,
            DifficultyLevel.Expert => 0.20,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };

        var crossBlockProbability = difficulty switch
        {
            DifficultyLevel.Hard => 0.75,
            DifficultyLevel.Expert => 1.0,
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
