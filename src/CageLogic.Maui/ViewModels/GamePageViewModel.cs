using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CageLogic.Maui.ViewModels;

public partial class GamePageViewModel : ObservableObject
{
	private readonly GameSession _session;

	[ObservableProperty]
	public partial GameSessionViewState ViewState { get; set; } = null!;

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	public GamePageViewModel(GameSessionStore sessionStore)
	{
		ArgumentNullException.ThrowIfNull(sessionStore);
		_session = sessionStore.Current ?? throw new InvalidOperationException("A game session must be created before navigating to the board.");
		ViewState = _session.ViewState;
	}

	public string DifficultyLabel => ViewState.Difficulty.ToString();

	public void SelectCell(CellPosition position)
	{
		_session.SelectCell(position);
		PublishState();
	}

	public void EnterDigit(int digit)
	{
		if (!_session.EnterDigit(digit))
			StatusMessage = "Selecione uma célula editável para inserir o dígito.";
		else
			StatusMessage = string.Empty;
		PublishState();
	}

	public void MoveSelection(int rowDelta, int columnDelta)
	{
		var current = ViewState.SelectedPosition ?? new CellPosition(0, 0);
		var row = current.Row + rowDelta;
		var column = current.Column + columnDelta;
		if (row is < 0 or > 8 || column is < 0 or > 8)
			return;

		SelectCell(new CellPosition(row, column));
	}

	[RelayCommand]
	private void ClearSelected()
	{
		if (!_session.ClearSelected())
			StatusMessage = "Selecione uma célula editável para apagar o valor.";
		else
			StatusMessage = string.Empty;
		PublishState();
	}

	private void PublishState()
	{
		ViewState = _session.ViewState;
		OnPropertyChanged(nameof(DifficultyLabel));
	}
}
