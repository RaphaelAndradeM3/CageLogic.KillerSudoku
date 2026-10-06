using CageLogic.Domain.Board;

namespace CageLogic.Domain.LogicalSteps.Techniques;

public sealed class HiddenPairTechnique : ILogicalTechnique
{
    public LogicalTechniqueId Id => LogicalTechniqueId.HiddenPair;

    public LogicalStep? FindStep(LogicalState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        var steps = new List<LogicalStep>();
        foreach (var region in SudokuRegions.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cells = region.Positions
                .Where(position => !state.Board.GetCell(position).CurrentValue.HasValue)
                .ToArray();
            var positionsByDigit = Enumerable.Range(1, 9).ToDictionary(
                digit => digit,
                digit => cells.Where(position => state.GetCandidates(position).Values.Contains(digit)).ToHashSet());
            for (var firstDigit = 1; firstDigit <= 9; firstDigit++)
            {
                var pairPositions = positionsByDigit[firstDigit];
                if (pairPositions.Count != 2)
                {
                    continue;
                }

                for (var secondDigit = firstDigit + 1; secondDigit <= 9; secondDigit++)
                {
                    if (!pairPositions.SetEquals(positionsByDigit[secondDigit]))
                    {
                        continue;
                    }

                    var eliminations = pairPositions
                        .OrderBy(LogicalStepOrdering.PositionIndex)
                        .SelectMany(position => state.GetCandidates(position).Values
                            .Where(value => value != firstDigit && value != secondDigit)
                            .Select(value => new CandidateElimination(position, value)))
                        .ToArray();
                    if (eliminations.Length > 0)
                    {
                        steps.Add(new LogicalStep(
                            Id,
                            eliminations: eliminations,
                            relatedPositions: pairPositions.Concat(eliminations.Select(item => item.Position))));
                    }
                }
            }
        }

        return LogicalStepOrdering.SelectBest(steps);
    }
}
