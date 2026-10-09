using CageLogic.Maui.Controls;
using CageLogic.Maui.Lifecycle;
using CageLogic.Maui.ViewModels;
using Microsoft.Maui.Accessibility;

namespace CageLogic.Maui.Views;

public partial class GamePage : ContentPage
{
	private readonly GamePageViewModel _viewModel;
	private readonly BoardInputBehavior _inputBehavior = new();
	private readonly GameSessionLifecycleBehavior _lifecycle = new();
	private bool _summaryNavigationStarted;

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

	protected override void OnAppearing()
	{
		base.OnAppearing();
		if (Window is not null)
			_lifecycle.Attach(Window, _viewModel.Session);
	}

	protected override void OnDisappearing()
	{
		_lifecycle.Detach(pause: true);
		base.OnDisappearing();
	}

	private void OnCellSelected(object? sender, BoardCellSelectedEventArgs eventArgs) =>
		_viewModel.SelectCell(eventArgs.Position);

	private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs eventArgs)
	{
		if (eventArgs.PropertyName == nameof(GamePageViewModel.IsHintPending) && _viewModel.IsHintPending)
			SemanticScreenReader.Default.Announce("Calculando dica. Você ainda pode editar o tabuleiro.");

		if (eventArgs.PropertyName == nameof(GamePageViewModel.ViewState))
		{
			BoardView.SetViewState(_viewModel.ViewState);
			if (_viewModel.ViewState.Summary is not null && !_summaryNavigationStarted)
			{
				_summaryNavigationStarted = true;
				MainThread.BeginInvokeOnMainThread(async () => await Shell.Current.GoToAsync(nameof(SessionSummaryPage)));
			}
		}
	}

	private async void OnDigitClicked(object? sender, EventArgs eventArgs)
	{
		if (sender is Button button && int.TryParse(button.Text, out var digit))
			await _viewModel.EnterDigitAsync(digit);
	}

	private async void OnKeyPressed(object? sender, BoardKeyInputEventArgs eventArgs)
	{
		if (eventArgs.ControlPressed)
		{
			if (eventArgs.Key.Equals("Z", StringComparison.OrdinalIgnoreCase))
				_viewModel.UndoCommand.Execute(null);
			else if (eventArgs.Key.Equals("Y", StringComparison.OrdinalIgnoreCase))
				_viewModel.RedoCommand.Execute(null);
			return;
		}

		if (BoardInputBehavior.TryGetDigit(eventArgs.Key, out var digit))
		{
			await _viewModel.EnterDigitAsync(digit);
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

	private async void OnBackClicked(object? sender, EventArgs eventArgs) => await Shell.Current.GoToAsync("..");
}
