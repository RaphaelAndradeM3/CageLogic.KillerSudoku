using System.Collections.ObjectModel;
using CageLogic.Domain.Board;
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
                    "A soma dos dígitos de uma linha, coluna ou bloco restringe as combinações que as cages internas e atravessadas podem formar."),
                [LogicalTechniqueId.NakedPair] = new(
                    "Par nu",
                    "Quando duas células de uma região compartilham exatamente duas possibilidades, essas possibilidades pertencem ao par e não podem aparecer nas demais células da região."),
                [LogicalTechniqueId.HiddenPair] = new(
                    "Par oculto",
                    "Quando dois dígitos só podem ocupar as mesmas duas células de uma região, essas células ficam reservadas para o par."),
                [LogicalTechniqueId.NakedTriple] = new(
                    "Tripla nua",
                    "Quando três células de uma região compartilham exatamente três possibilidades, elas ficam reservadas para essa tripla.")
            });

    public LogicalHintExplanation Get(LogicalTechniqueId techniqueId)
    {
        if (!Entries.TryGetValue(techniqueId, out var explanation))
        {
            throw new ArgumentOutOfRangeException(nameof(techniqueId), techniqueId, "No explanation is registered for this logical technique.");
        }

        return explanation;
    }

    /// <summary>Formats available typed evidence in Portuguese without revealing a placement before level 3.</summary>
    public string Format(
        LogicalTechniqueId techniqueId,
        LogicalStepEvidence evidence,
        LogicalPlacement? placement,
        HintLevel level)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        var explanation = Get(techniqueId).Explanation;
        if (level == HintLevel.Explanation)
        {
            return explanation;
        }

        var details = new List<string>();
        if (evidence.ScopeContext is { } scopeContext)
        {
            details.Add(FormatScope(scopeContext));
        }

        var canRevealDigits = level == HintLevel.Action || placement is null;
        if (canRevealDigits && evidence.RelevantDigits.Count > 0)
        {
            details.Add(FormatDigits(evidence.RelevantDigits));
        }

        if (canRevealDigits && evidence.PatternCandidates.Count > 0)
        {
            details.Add(FormatPatternCandidates(evidence.PatternCandidates));
        }

        return details.Count == 0
            ? explanation
            : $"{explanation} {string.Join(" ", details)}";
    }

    private static string FormatScope(LogicalScopeContext context)
    {
        if (context.Kind == LogicalScopeKind.Cage)
        {
            return $"O escopo é a cage com soma-alvo {context.TargetSum}.";
        }

        var region = context.RegionKind is { } regionKind && context.RegionIndex is { } regionIndex
            ? GetRegionPhrase(regionKind, regionIndex)
            : null;
        if (context.Kind == LogicalScopeKind.CageRegionIntersection)
        {
            return $"O escopo é a interseção entre a cage com soma-alvo {context.TargetSum} e {region ?? "a região"}.";
        }

        if (context.Kind == LogicalScopeKind.RuleOf45)
        {
            var regionAfterPreposition = context.RegionKind is { } kind && context.RegionIndex is { } index
                ? GetRegionAfterPreposition(kind, index)
                : "esta região";
            return $"Na regra do 45 aplicada {regionAfterPreposition}, o resíduo é {context.ResidualSum} após considerar {FormatCageTargets(context.RelatedCageTargetSums)}.";
        }

        return $"O escopo é {region ?? "a região"}.";
    }

    private static string FormatDigits(IReadOnlyList<int> digits)
    {
        var list = FormatList(digits.Select(digit => digit.ToString()));
        return digits.Count == 1
            ? $"O dígito envolvido é {list}."
            : $"Os dígitos envolvidos são {list}.";
    }

    private static string FormatPatternCandidates(IReadOnlyList<LogicalPatternCandidate> candidates)
    {
        var pairs = candidates
            .GroupBy(candidate => candidate.Value)
            .OrderBy(group => group.Key)
            .Select(group => $"o dígito {group.Key} nas células {FormatList(group
                .OrderBy(candidate => candidate.Position.Row * 9 + candidate.Position.Column)
                .Select(candidate => $"({candidate.Position.Row + 1},{candidate.Position.Column + 1})"))}");
        return $"No padrão, os pares posição/dígito são: {FormatList(pairs)}.";
    }

    private static string FormatCageTargets(IReadOnlyList<int> targetSums)
    {
        if (targetSums.Count == 0)
        {
            return "as cages relacionadas";
        }

        var cages = targetSums
            .GroupBy(target => target)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Count()} {(group.Count() == 1 ? "cage" : "cages")} com soma-alvo {group.Key}");
        return FormatList(cages);
    }

    private static string FormatList(IEnumerable<string> items)
    {
        var values = items.ToArray();
        return values.Length switch
        {
            0 => string.Empty,
            1 => values[0],
            2 => $"{values[0]} e {values[1]}",
            _ => $"{string.Join(", ", values[..^1])} e {values[^1]}"
        };
    }

    private static string GetRegionPhrase(LogicalScopeKind regionKind, int index) => regionKind switch
    {
        LogicalScopeKind.Row => $"a linha {index + 1}",
        LogicalScopeKind.Column => $"a coluna {index + 1}",
        LogicalScopeKind.Block => $"o bloco {index + 1}",
        _ => throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Expected a row, column, or block scope.")
    };

    private static string GetRegionAfterPreposition(LogicalScopeKind regionKind, int index) => regionKind switch
    {
        LogicalScopeKind.Row => $"à linha {index + 1}",
        LogicalScopeKind.Column => $"à coluna {index + 1}",
        LogicalScopeKind.Block => $"ao bloco {index + 1}",
        _ => throw new ArgumentOutOfRangeException(nameof(regionKind), regionKind, "Expected a row, column, or block scope.")
    };
}

public sealed record LogicalHintExplanation(string Name, string Explanation);
