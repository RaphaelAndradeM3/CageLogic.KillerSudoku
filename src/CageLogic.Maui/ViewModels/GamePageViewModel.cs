using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;
using CageLogic.Domain.Board;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;

namespace CageLogic.Maui.ViewModels;

public partial class GamePageViewModel : ObservableObject, IDisposable
{
	private readonly GameSession _session;
	private readonly IDispatcherTimer? _elapsedTimer;
	private long _lastBoardRevision;
	private bool _disposed;

	[ObservableProperty]
	public partial GameSessionViewState ViewState { get; set; } = null!;

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string HintMessage { get; set; } = string.Empty;

	public GamePageViewModel(GameSessionStore sessionStore)
	{
		ArgumentNullException.ThrowIfNull(sessionStore);
		_session = sessionStore.Current ?? throw new InvalidOperationException("A game session must be created before navigating to the board.");
		ViewState = _session.ViewState;
		_session.ViewStateChanged += OnSessionViewStateChanged;
		_elapsedTimer = Microsoft.Maui.Controls.Application.Current?.Dispatcher.CreateTimer();
		if (_elapsedTimer is not null)
		{
			_elapsedTimer.Interval = TimeSpan.FromSeconds(1);
			_elapsedTimer.Tick += OnElapsedTimerTick;
			_elapsedTimer.Start();
		}
	}

	public string DifficultyLabel => ViewState.Difficulty.ToString();
	public GameSession Session => _session;
	public string PauseButtonText => ViewState.IsPaused ? "Retomar tempo" : "Pausar tempo";

	public void SelectCell(CellPosition position)
	{
		_session.SelectCell(position);
		PublishState();
	}

	public async Task EnterDigitAsync(int digit)
	{
		var applied = await Task.Run(() => _session.EnterDigit(digit)).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			StatusMessage = applied ? string.Empty : "Selecione uma célula editável e vazia no modo de candidatos.";
			PublishState();
		});
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
	private void ToggleInputMode()
	{
		_session.SetInputMode(_session.InputMode == GameInputMode.Answer ? GameInputMode.Candidate : GameInputMode.Answer);
		PublishState();
	}

	[RelayCommand]
	private void ClearSelected()
	{
		StatusMessage = _session.ClearSelected() ? string.Empty : "A célula selecionada não pode ser apagada neste modo.";
		PublishState();
	}

	[RelayCommand]
	private async Task AutoFillCandidatesAsync(CancellationToken cancellationToken)
	{
		var changed = await Task.Run(_session.AutoFillCandidates, cancellationToken).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			StatusMessage = changed ? "Candidatos preenchidos para as células vazias." : "Os candidatos já estavam atualizados.";
			PublishState();
		});
	}

	[RelayCommand]
	private void Undo()
	{
		StatusMessage = _session.Undo() ? string.Empty : "Não há ação para desfazer.";
		PublishState();
	}

	[RelayCommand]
	private void Redo()
	{
		StatusMessage = _session.Redo() ? string.Empty : "Não há ação para refazer.";
		PublishState();
	}

	[RelayCommand]
	private void TogglePause()
	{
		if (_session.IsPaused)
			_session.Resume();
		else
			_session.Pause();
		PublishState();
	}

	[RelayCommand]
	private async Task RequestHintAsync(CancellationToken cancellationToken)
	{
		var hint = await _session.RequestNextHintAsync(cancellationToken).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			HintMessage = hint?.Status switch
			{
				HintStatus.Available => hint.Explanation ?? string.Empty,
				HintStatus.NoSafeHint => "Não há uma dica segura para esta posição.",
				HintStatus.InconsistentState => "Corrija os conflitos para pedir uma dica.",
				HintStatus.PuzzleSolved => "O tabuleiro já está resolvido.",
				HintStatus.ValueNotConfirmed => "Não foi possível confirmar esta ação.",
				_ => "A solicitação de dica foi cancelada."
			};
			PublishState();
		});
	}

	[RelayCommand]
	private void Complete()
	{
		var result = _session.TryComplete();
		StatusMessage = result.Status switch
		{
			SessionCompletionStatus.Completed => string.Empty,
			SessionCompletionStatus.Incomplete => "Preencha todas as células antes de concluir.",
			SessionCompletionStatus.ConflictingBoard => "Corrija os conflitos antes de concluir.",
			SessionCompletionStatus.IncorrectSolution => "A solução não corresponde ao quebra-cabeça.",
			_ => string.Empty
		};
		PublishState();
	}

	private void OnSessionViewStateChanged(object? sender, EventArgs eventArgs) =>
		MainThread.BeginInvokeOnMainThread(PublishState);

	private void OnElapsedTimerTick(object? sender, EventArgs eventArgs)
	{
		if (!_session.IsPaused && _session.Summary is null)
			PublishState();
	}

	private void PublishState()
	{
		var nextState = _session.ViewState;
		if (nextState.BoardRevision != _lastBoardRevision)
		{
			_lastBoardRevision = nextState.BoardRevision;
			HintMessage = string.Empty;
		}
		ViewState = nextState;
		if (nextState.Hint is { } hint)
			HintMessage = FormatHint(hint);
		OnPropertyChanged(nameof(DifficultyLabel));
		OnPropertyChanged(nameof(PauseButtonText));
	}

	private static string FormatHint(HintResult? hint) => hint?.Status switch
	{
		HintStatus.Available => hint.Explanation ?? string.Empty,
		HintStatus.NoSafeHint => "Não há uma dica segura para esta posição.",
		HintStatus.InconsistentState => "Corrija os conflitos para pedir uma dica.",
		HintStatus.PuzzleSolved => "O tabuleiro já está resolvido.",
		HintStatus.ValueNotConfirmed => "Não foi possível confirmar esta ação.",
		_ => string.Empty
	};

	public void Dispose()
	{
		if (_disposed)
			return;
		_session.ViewStateChanged -= OnSessionViewStateChanged;
		if (_elapsedTimer is not null)
		{
			_elapsedTimer.Stop();
			_elapsedTimer.Tick -= OnElapsedTimerTick;
		}
		_disposed = true;
	}
}
