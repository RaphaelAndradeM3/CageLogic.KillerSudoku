using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;
using CageLogic.Application.Progression;
using CageLogic.Domain.Board;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Dispatching;

namespace CageLogic.Maui.ViewModels;

public partial class GamePageViewModel : ObservableObject, IDisposable
{
	private readonly GameSession _session;
	private readonly GameSessionStore _sessionStore;
	private readonly GameProgressCommandQueue _progressQueue;
	private readonly CompleteGameProgressUseCase _completeGame;
	private readonly AbandonGameProgressUseCase _abandonGame;
	private readonly ILogger<GamePageViewModel> _logger;
	private readonly IDispatcherTimer? _elapsedTimer;
	private long _lastBoardRevision;
	private int _pendingWrites;
	private string _confirmedPersistenceStatus;
	private bool _disposed;

	[ObservableProperty]
	public partial GameSessionViewState ViewState { get; set; } = null!;

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string PersistenceStatus { get; set; } = string.Empty;

	[ObservableProperty]
	public partial string HintMessage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool IsHintPending { get; set; }

	[ObservableProperty]
	public partial bool IsSaving { get; set; }

	[ObservableProperty]
	public partial bool IsCompletionPersisted { get; set; }

	public GamePageViewModel(
		GameSessionStore sessionStore,
		CompleteGameProgressUseCase completeGame,
		AbandonGameProgressUseCase abandonGame,
		ILogger<GamePageViewModel> logger)
	{
		ArgumentNullException.ThrowIfNull(sessionStore);
		ArgumentNullException.ThrowIfNull(completeGame);
		ArgumentNullException.ThrowIfNull(abandonGame);
		ArgumentNullException.ThrowIfNull(logger);
		_sessionStore = sessionStore;
		_session = sessionStore.Current ?? throw new InvalidOperationException("A game session must be created before navigating to the board.");
		_progressQueue = sessionStore.Commands ?? throw new InvalidOperationException("A persistence queue must be created before navigating to the board.");
		_completeGame = completeGame;
		_abandonGame = abandonGame;
		_logger = logger;
		ViewState = _session.ViewState;
		IsHintPending = _session.IsHintPending;
		_confirmedPersistenceStatus = sessionStore.IsPersisted ? "Salvo." : "Falha ao salvar.";
		PersistenceStatus = _confirmedPersistenceStatus;
		if (!sessionStore.IsPersisted)
			StatusMessage = "O primeiro salvamento falhou. A partida continua na memória, mas ainda não pode ser retomada após fechar o aplicativo.";
		_elapsedTimer = Microsoft.Maui.Controls.Application.Current?.Dispatcher.CreateTimer();
		if (_elapsedTimer is not null)
		{
			_elapsedTimer.Interval = TimeSpan.FromSeconds(1);
			_elapsedTimer.Tick += OnElapsedTimerTick;
			_elapsedTimer.Start();
		}
		_session.ViewStateChanged += OnSessionViewStateChanged;
	}

	public string DifficultyLabel => ViewState.Difficulty.ToString();
	public GameSession Session => _session;
	public string PauseButtonText => ViewState.IsPaused ? "Retomar tempo" : "Pausar tempo";
	public string CompleteButtonText => _session.Summary is null ? "Concluir partida" : "Tentar salvar conclusão";

	public void SelectCell(CellPosition position)
	{
		_session.SelectCell(position);
		PublishState();
	}

	public async Task EnterDigitAsync(int digit, CancellationToken cancellationToken = default)
	{
		await ExecuteRecoverablyAsync(async () =>
		{
			var intent = ViewState;
			if (intent.SelectedPosition is not { } position)
			{
				await MainThread.InvokeOnMainThreadAsync(() => StatusMessage = "Selecione uma célula antes de informar um dígito.");
				return;
			}

			var result = await ExecutePersistedAsync(
				() => _session.EnterDigit(position, intent.InputMode, digit), cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (!result.Applied && !IsSaving)
					StatusMessage = "A célula selecionada não aceita essa alteração.";
				PublishState();
			});
		}, cancellationToken).ConfigureAwait(false);
	}

	public void MoveSelection(int rowDelta, int columnDelta)
	{
		if (ViewState.SelectedPosition is not { } current)
		{
			SelectCell(new CellPosition(0, 0));
			return;
		}

		var row = current.Row + rowDelta;
		var column = current.Column + columnDelta;
		if (row is < 0 or > 8 || column is < 0 or > 8)
			return;

		SelectCell(new CellPosition(row, column));
	}

	[RelayCommand]
	private void ToggleInputMode()
	{
		try
		{
			_session.SetInputMode(_session.InputMode == GameInputMode.Answer ? GameInputMode.Candidate : GameInputMode.Answer);
			PublishState();
		}
		catch (Exception exception)
		{
			ReportUnexpectedFailure(exception);
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task ClearSelectedAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(() => ClearSelectedCoreAsync(cancellationToken), cancellationToken);

	private async Task ClearSelectedCoreAsync(CancellationToken cancellationToken)
	{
		var intent = ViewState;
		if (intent.SelectedPosition is not { } position)
		{
			StatusMessage = "Selecione uma célula antes de apagar.";
			return;
		}
		var result = await ExecutePersistedAsync(
			() => _session.ClearSelected(position, intent.InputMode), cancellationToken).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (!result.Applied && !IsSaving)
				StatusMessage = "A célula selecionada não pode ser apagada neste modo.";
			PublishState();
		});
	}

	[RelayCommand]
	private Task AutoFillCandidatesAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(() => AutoFillCandidatesCoreAsync(cancellationToken), cancellationToken);

	private async Task AutoFillCandidatesCoreAsync(CancellationToken cancellationToken)
	{
		var result = await ExecutePersistedAsync(_session.AutoFillCandidates, cancellationToken).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (!result.Applied && !IsSaving)
				StatusMessage = "Os candidatos já estavam atualizados.";
			PublishState();
		});
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task UndoAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(() => HistoryCoreAsync(undo: true, cancellationToken), cancellationToken);

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task RedoAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(() => HistoryCoreAsync(undo: false, cancellationToken), cancellationToken);

	private async Task HistoryCoreAsync(bool undo, CancellationToken cancellationToken)
	{
		var result = await ExecutePersistedAsync(undo ? _session.Undo : _session.Redo, cancellationToken).ConfigureAwait(false);
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			if (!result.Applied && !IsSaving)
				StatusMessage = undo ? "Não há ação para desfazer." : "Não há ação para refazer.";
			PublishState();
		});
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task TogglePauseAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(async () =>
		{
			if (_session.IsPaused)
				_session.Resume();
			else
				_session.Pause();
			PublishState();
			await PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
		}, cancellationToken);

	[RelayCommand(AllowConcurrentExecutions = false)]
	private async Task RequestHintAsync(CancellationToken cancellationToken)
	{
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			StatusMessage = string.Empty;
			HintMessage = string.Empty;
			IsHintPending = true;
		});

		try
		{
			var hint = await _session.RequestNextHintAsync(cancellationToken).ConfigureAwait(false);
			if (hint is not null && hint.BoardRevision != _session.BoardRevision)
				hint = null;
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HintMessage = hint is null ? string.Empty : FormatHint(hint);
				PublishState();
			});

			if (hint?.Status == HintStatus.Available)
				await PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
				HintMessage = _session.IsHintPending ? string.Empty : "A solicitação de dica foi cancelada.");
		}
		catch (Exception exception)
		{
			var correlationId = Guid.NewGuid().ToString("N");
			_logger.LogError(
				exception,
				"Hint calculation failed for game session {SessionId} (CorrelationId {CorrelationId})",
				_progressQueue.ActiveRecord.SessionId,
				correlationId);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HintMessage = string.Empty;
				StatusMessage = $"Não foi possível calcular a dica. Tente novamente. Código: {correlationId}";
			});
		}
		finally
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				IsHintPending = _session.IsHintPending;
				PublishState();
			});
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task CompleteAsync(CancellationToken cancellationToken) =>
		ExecuteRecoverablyAsync(() => CompleteCoreAsync(cancellationToken), cancellationToken);

	private async Task CompleteCoreAsync(CancellationToken cancellationToken)
	{
		await BeginWriteAsync();
		try
		{
			var savedCurrent = await _progressQueue.PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() => SetConfirmedPersistenceStatus(savedCurrent.IsConfirmed));
			if (!savedCurrent.IsConfirmed)
			{
				await MainThread.InvokeOnMainThreadAsync(() => StatusMessage = "A conclusão não foi salva. A sessão continua na memória; tente salvar novamente.");
				return;
			}

			var result = await _progressQueue.ExecuteExclusiveAsync(
				() => _completeGame.ExecuteAsync(_session, _progressQueue.ActiveRecord, cancellationToken), cancellationToken)
				.ConfigureAwait(false);
			if (result.Persistence?.IsSaved == true && result.Record is not null)
			{
				_sessionStore.UpdateRecord(result.Record);
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					SetConfirmedPersistenceStatus(isSaved: true);
					IsCompletionPersisted = true;
					StatusMessage = string.Empty;
					PublishState();
				});
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (result.Completion.Status == SessionCompletionStatus.Completed)
					SetConfirmedPersistenceStatus(isSaved: false);
				IsCompletionPersisted = false;
				StatusMessage = result.Completion.Status switch
				{
					SessionCompletionStatus.Incomplete => "Preencha todas as células antes de concluir.",
					SessionCompletionStatus.ConflictingBoard => "Corrija os conflitos antes de concluir.",
					SessionCompletionStatus.IncorrectSolution => "A solução não corresponde ao quebra-cabeça.",
					SessionCompletionStatus.Completed => "A solução está correta, mas não foi possível salvar a conclusão. Tente novamente.",
					_ => string.Empty
				};
				PublishState();
			});
		}
		finally
		{
			await EndWriteAsync();
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private async Task AbandonAsync(CancellationToken cancellationToken)
	{
		var confirmed = await Shell.Current.DisplayAlertAsync(
			"Abandonar partida?",
			"A partida será registrada como abandonada e não poderá ser retomada.",
			"Abandonar",
			"Continuar jogando");
		if (!confirmed)
			return;

		await BeginWriteAsync();
		try
		{
			var savedCurrent = await _progressQueue.PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() => SetConfirmedPersistenceStatus(savedCurrent.IsConfirmed));
			if (!savedCurrent.IsConfirmed)
			{
				await MainThread.InvokeOnMainThreadAsync(() => StatusMessage = "Não foi possível salvar o abandono. A partida continua disponível na memória.");
				return;
			}

			var result = await _progressQueue.ExecuteExclusiveAsync(
				() => _abandonGame.ExecuteAsync(_session, _progressQueue.ActiveRecord, cancellationToken), cancellationToken)
				.ConfigureAwait(false);
			if (!result.Persistence.IsSaved)
			{
				await MainThread.InvokeOnMainThreadAsync(() => SetConfirmedPersistenceStatus(isSaved: false));
				await MainThread.InvokeOnMainThreadAsync(() => StatusMessage = "Não foi possível abandonar a partida. Tente novamente.");
				return;
			}

			await MainThread.InvokeOnMainThreadAsync(() => SetConfirmedPersistenceStatus(isSaved: true));
			_sessionStore.UpdateRecord(result.Record);
			_sessionStore.Clear();
			await Shell.Current.GoToAsync("//home");
		}
		finally
		{
			await EndWriteAsync();
		}
	}

	public async Task PersistOnPauseAsync()
	{
		if (_session.Summary is not null || _sessionStore.ActiveRecord?.Status != GameProgressStatus.Active)
			return;
		_session.PauseForBackground();
		try
		{
			await PersistCurrentAsync(CancellationToken.None).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			ReportUnexpectedFailure(exception);
		}
	}

	private async Task<GameProgressCommandResult> ExecutePersistedAsync(
		Func<bool> mutation,
		CancellationToken cancellationToken)
	{
		await BeginWriteAsync();
		try
		{
			var result = await _progressQueue.ExecuteAsync(mutation, cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				if (result.Applied)
				{
					SetConfirmedPersistenceStatus(result.IsConfirmed);
					PersistenceStatus = result.IsConfirmed && _pendingWrites > 1 ? "Salvando..." : _confirmedPersistenceStatus;
					StatusMessage = result.IsConfirmed
						? (_pendingWrites > 1 ? "Salvando..." : string.Empty)
						: "Alteração ainda não salva. A partida continua na memória; tente outra alteração para repetir o salvamento.";
				}
			});
			return result;
		}
		finally
		{
			await EndWriteAsync();
		}
	}

	private async Task PersistCurrentAsync(CancellationToken cancellationToken)
	{
		await BeginWriteAsync();
		try
		{
			var result = await _progressQueue.PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				SetConfirmedPersistenceStatus(result.IsConfirmed);
				StatusMessage = result.IsConfirmed ? string.Empty : "O salvamento falhou. Alterações recentes ainda não estão salvas.";
			});
		}
		finally
		{
			await EndWriteAsync();
		}
	}

	private async Task BeginWriteAsync() => await MainThread.InvokeOnMainThreadAsync(() =>
	{
		_pendingWrites++;
		IsSaving = true;
		PersistenceStatus = "Salvando...";
		StatusMessage = "Salvando...";
	});

	private async Task EndWriteAsync() => await MainThread.InvokeOnMainThreadAsync(() =>
	{
		_pendingWrites = Math.Max(0, _pendingWrites - 1);
		IsSaving = _pendingWrites > 0;
		if (!IsSaving)
		{
			PersistenceStatus = _confirmedPersistenceStatus;
			if (StatusMessage == "Salvando...")
				StatusMessage = string.Empty;
		}
	});

	private void SetConfirmedPersistenceStatus(bool isSaved)
	{
		_confirmedPersistenceStatus = isSaved ? "Salvo." : "Falha ao salvar.";
		PersistenceStatus = _pendingWrites > 1 && isSaved ? "Salvando..." : _confirmedPersistenceStatus;
	}

	private void OnSessionViewStateChanged(object? sender, EventArgs eventArgs) =>
		MainThread.BeginInvokeOnMainThread(() =>
		{
			IsHintPending = _session.IsHintPending;
			PublishState();
		});

	private void OnElapsedTimerTick(object? sender, EventArgs eventArgs)
	{
		if (!_session.IsPaused && _session.Summary is null)
			PublishState();
	}

	private void PublishState()
	{
		var nextState = _session.ViewState;
		IsHintPending = _session.IsHintPending;
		if (_session.HintAnalysisFailed)
			StatusMessage = "Não foi possível calcular a dica. Tente novamente.";
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
		OnPropertyChanged(nameof(CompleteButtonText));
	}

	public void ReportUnexpectedFailure(Exception exception)
	{
		ArgumentNullException.ThrowIfNull(exception);
		var correlationId = Guid.NewGuid().ToString("N");
		_logger.LogError(
			exception,
			"Recoverable game action failed for game session {SessionId} (CorrelationId {CorrelationId})",
			_progressQueue.ActiveRecord.SessionId,
			correlationId);
		MainThread.BeginInvokeOnMainThread(() =>
			StatusMessage = $"Ocorreu um erro inesperado. Tente novamente. Código: {correlationId}");
	}

	private async Task ExecuteRecoverablyAsync(Func<Task> action, CancellationToken cancellationToken = default)
	{
		try
		{
			await action();
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// View teardown cancels queued work and is not a player-visible failure.
		}
		catch (Exception exception)
		{
			ReportUnexpectedFailure(exception);
		}
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
