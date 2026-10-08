using CageLogic.Application.GameSessions;

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
		return $"Linha {cell.Position.Row + 1}, coluna {cell.Position.Column + 1}. {value}. {fixedState}. Sem notas. {selection}. {conflict}. Sem destaque de dica.";
	}
}
