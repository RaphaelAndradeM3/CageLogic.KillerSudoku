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
	private bool _cleanedUp;

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
		if (Shell.Current is { } shell)
			shell.Navigated += OnShellNavigated;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.Session.Start();
		if (Window is not null)
			_lifecycle.Attach(Window, _viewModel.Session);
	}

	protected override void OnDisappearing()
	{
		_lifecycle.Detach(pause: true);
		base.OnDisappearing();
	}

	protected override bool OnBackButtonPressed()
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await Shell.Current.GoToAsync("..");
			}
			catch (Exception exception)
			{
				_viewModel.ReportUnexpectedFailure(exception);
			}
		});
		return true;
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
				MainThread.BeginInvokeOnMainThread(async () =>
					await RunRecoverableActionAsync(() => Shell.Current.GoToAsync(nameof(SessionSummaryPage))));
			}
		}
	}

	private async void OnDigitClicked(object? sender, EventArgs eventArgs)
	{
		if (sender is Button button && int.TryParse(button.Text, out var digit))
			await RunRecoverableActionAsync(() => _viewModel.EnterDigitAsync(digit));
	}

	private async void OnKeyPressed(object? sender, BoardKeyInputEventArgs eventArgs)
	{
		await RunRecoverableActionAsync(async () =>
		{
			if (eventArgs.ControlPressed)
			{
				if (eventArgs.Key.Equals("Z", StringComparison.OrdinalIgnoreCase))
					await _viewModel.UndoCommand.ExecuteAsync(null);
				else if (eventArgs.Key.Equals("Y", StringComparison.OrdinalIgnoreCase))
					await _viewModel.RedoCommand.ExecuteAsync(null);
				return;
			}

			if (BoardInputBehavior.TryGetDigit(eventArgs.Key, out var digit))
			{
				await _viewModel.EnterDigitAsync(digit);
				return;
			}

			if (BoardInputBehavior.TryGetDirection(eventArgs.Key, out var rowDelta, out var columnDelta))
				_viewModel.MoveSelection(rowDelta, columnDelta);
			else if (BoardInputBehavior.IsClearKey(eventArgs.Key))
				await _viewModel.ClearSelectedCommand.ExecuteAsync(null);
		});
	}

	private async void OnBackClicked(object? sender, EventArgs eventArgs) =>
		await RunRecoverableActionAsync(() => Shell.Current.GoToAsync(".."));

	private async Task RunRecoverableActionAsync(Func<Task> action)
	{
		try
		{
			await action();
		}
		catch (Exception exception)
		{
			_viewModel.ReportUnexpectedFailure(exception);
		}
	}

	private void OnShellNavigated(object? sender, ShellNavigatedEventArgs eventArgs)
	{
		var route = eventArgs.Current.Location.OriginalString.TrimEnd('/');
		var currentSegment = route.Split('/').LastOrDefault()?.Split('?')[0];
		if (string.Equals(currentSegment, nameof(GamePage), StringComparison.OrdinalIgnoreCase))
			return;

		try
		{
			if (string.Equals(currentSegment, nameof(SessionSummaryPage), StringComparison.OrdinalIgnoreCase) &&
				Navigation.NavigationStack.Contains(this))
				Navigation.RemovePage(this);
		}
		catch (Exception exception)
		{
			_viewModel.ReportUnexpectedFailure(exception);
		}
		finally
		{
			Cleanup();
		}
	}

	private void Cleanup()
	{
		if (_cleanedUp)
			return;

		_cleanedUp = true;
		if (Shell.Current is { } shell)
			shell.Navigated -= OnShellNavigated;
		_lifecycle.Dispose();
		BoardView.CellSelected -= OnCellSelected;
		_viewModel.PropertyChanged -= OnViewModelPropertyChanged;
		_inputBehavior.KeyPressed -= OnKeyPressed;
		Behaviors.Remove(_inputBehavior);
		_viewModel.Dispose();
	}
}
