using CageLogic.Maui.Controls;
using CageLogic.Maui.ViewModels;

namespace CageLogic.Maui.Views;

public partial class GamePage : ContentPage
{
	private readonly GamePageViewModel _viewModel;
	private readonly BoardInputBehavior _inputBehavior = new();

	public GamePage(GamePageViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
		BoardView.CellSelected += OnCellSelected;
		_viewModel.PropertyChanged += OnViewModelPropertyChanged;
		_inputBehavior.KeyPressed += OnKeyPressed;
		Behaviors.Add(_inputBehavior);
		BoardView.SetViewState(_viewModel.ViewState);
	}

	private void OnCellSelected(object? sender, BoardCellSelectedEventArgs eventArgs)
	{
		_viewModel.SelectCell(eventArgs.Position);
	}

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
	{
		if (eventArgs.PropertyName == nameof(GamePageViewModel.ViewState))
			BoardView.SetViewState(_viewModel.ViewState);
	}

	private void OnDigitClicked(object? sender, EventArgs eventArgs)
	{
		if (sender is Button button && int.TryParse(button.Text, out var digit))
			_viewModel.EnterDigit(digit);
	}

	private void OnKeyPressed(object? sender, BoardKeyInputEventArgs eventArgs)
	{
		if (eventArgs.ControlPressed)
			return;

		if (BoardInputBehavior.TryGetDigit(eventArgs.Key, out var digit))
		{
			_viewModel.EnterDigit(digit);
			return;
		}

		if (eventArgs.Key.Contains("Left", StringComparison.OrdinalIgnoreCase))
			_viewModel.MoveSelection(0, -1);
		else if (eventArgs.Key.Contains("Right", StringComparison.OrdinalIgnoreCase))
			_viewModel.MoveSelection(0, 1);
		else if (eventArgs.Key.Contains("Up", StringComparison.OrdinalIgnoreCase))
			_viewModel.MoveSelection(-1, 0);
		else if (eventArgs.Key.Contains("Down", StringComparison.OrdinalIgnoreCase))
			_viewModel.MoveSelection(1, 0);
		else if (eventArgs.Key.Contains("Backspace", StringComparison.OrdinalIgnoreCase) ||
			eventArgs.Key.Contains("Delete", StringComparison.OrdinalIgnoreCase) ||
			eventArgs.Key.Equals("Del", StringComparison.OrdinalIgnoreCase))
			_viewModel.ClearSelectedCommand.Execute(null);
	}

	private async void OnBackClicked(object? sender, EventArgs eventArgs)
	{
		await Shell.Current.GoToAsync("..");
	}
}
