using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;

namespace CageLogic.Maui.Controls;

/// <summary>Creates the nonvisual description announced for a board cell by screen readers.</summary>
public static class AccessibleCellPeer
{
	public static string Describe(GameSessionCellViewState cell)
	{
		ArgumentNullException.ThrowIfNull(cell);
		var value = cell.Value.HasValue ? $"valor {cell.Value.Value}" : "vazia";
		var fixedState = cell.IsGiven ? "fixa" : "editável";
		var selection = cell.IsSelected ? "selecionada" : "não selecionada";
		var conflict = cell.HasConflict ? "com conflito" : "sem conflito";
		var notes = cell.Notes.Count == 0 ? "Sem notas" : $"Notas {string.Join(", ", cell.Notes)}";
		var hints = cell.HintRoles.Count == 0
			? "Sem destaque de dica"
			: $"Dica: {string.Join(", ", cell.HintRoles.Select(DescribeRole))}";
		return $"Linha {cell.Position.Row + 1}, coluna {cell.Position.Column + 1}. {value}. {fixedState}. {notes}. {selection}. {conflict}. {hints}.";
	}

	private static string DescribeRole(HintHighlightRole role) => role switch
	{
		HintHighlightRole.Pattern => "padrão lógico",
		HintHighlightRole.Scope => "região analisada",
		HintHighlightRole.Target => "célula alvo",
		HintHighlightRole.Affected => "célula afetada",
		_ => "destaque"
	};
}
