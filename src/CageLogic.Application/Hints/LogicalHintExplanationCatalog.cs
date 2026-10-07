using System.Collections.ObjectModel;
using CageLogic.Domain.LogicalSteps;

namespace CageLogic.Application.Hints;

/// <summary>Portuguese, technique-level explanations kept outside the domain model.</summary>
public sealed class LogicalHintExplanationCatalog
{
    private static readonly IReadOnlyDictionary<LogicalTechniqueId, LogicalHintExplanation> Entries =
        new ReadOnlyDictionary<LogicalTechniqueId, LogicalHintExplanation>(
            new Dictionary<LogicalTechniqueId, LogicalHintExplanation>
            {
                [LogicalTechniqueId.NakedSingle] = new(
                    "Único candidato",
                    "Quando uma célula tem apenas uma possibilidade pelas regras do tabuleiro, essa possibilidade é obrigatória."),
                [LogicalTechniqueId.HiddenSingle] = new(
                    "Posição única",
                    "Quando um dígito só pode ocupar uma célula em uma linha, coluna ou bloco, essa posição é obrigatória."),
                [LogicalTechniqueId.CageSingle] = new(
                    "Valor único na cage",
                    "Ao considerar todas as combinações válidas da cage, uma célula mantém o mesmo valor em todas elas."),
                [LogicalTechniqueId.CageCombination] = new(
                    "Combinações da cage",
                    "Cada cage precisa completar seu alvo com dígitos distintos; combinações impossíveis são removidas das possibilidades das células."),
                [LogicalTechniqueId.CageRegionIntersection] = new(
                    "Interseção de cage e região",
                    "Se as posições possíveis de um dígito em uma cage estão na mesma região, esse dígito não pode aparecer nas outras células dela."),
                [LogicalTechniqueId.RuleOf45] = new(
                    "Regra do 45",
                    "A soma dos dígitos de uma linha, coluna ou bloco restringe as combinações que as cages internas e atravessadas podem formar.")
            });

    public LogicalHintExplanation Get(LogicalTechniqueId techniqueId)
    {
        if (!Entries.TryGetValue(techniqueId, out var explanation))
        {
            throw new ArgumentOutOfRangeException(nameof(techniqueId), techniqueId, "No explanation is registered for this logical technique.");
        }

        return explanation;
    }
}

public sealed record LogicalHintExplanation(string Name, string Explanation);
