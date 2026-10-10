using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Application.Progression;
using CageLogic.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace CageLogic.Maui.ViewModels;

public partial class HomeViewModel : ObservableObject
{
	private readonly GeneratePuzzleUseCase _generatePuzzle;
	private readonly GameSessionStore _sessionStore;
	private readonly LoadActiveGameUseCase _loadActiveGame;
	private readonly CreateGameProgressUseCase _createGameProgress;
	private readonly IGameProgressStore _progressStore;
	private readonly ILogger<HomeViewModel> _logger;
	private CancellationTokenSource? _generationCancellation;
	private string? _unrecoverableSessionKey;
	private bool _initialized;

	[ObservableProperty]
	public partial DifficultyLevel SelectedDifficulty { get; set; } = DifficultyLevel.Easy;

	[ObservableProperty]
	public partial bool IsGenerating { get; set; }

	[ObservableProperty]
	public partial bool IsInitializing { get; set; }

	[ObservableProperty]
	public partial bool HasActiveGame { get; set; }

	[ObservableProperty]
	public partial bool RecoveryRequired { get; set; }

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool HasRetryableFailure { get; set; }

	[ObservableProperty]
	public partial bool HasLoadFailure { get; set; }

	public HomeViewModel(
		GeneratePuzzleUseCase generatePuzzle,
		GameSessionStore sessionStore,
		LoadActiveGameUseCase loadActiveGame,
		CreateGameProgressUseCase createGameProgress,
		IGameProgressStore progressStore,
		ILogger<HomeViewModel> logger)
	{
		_generatePuzzle = generatePuzzle;
		_sessionStore = sessionStore;
		_loadActiveGame = loadActiveGame;
		_createGameProgress = createGameProgress;
		_progressStore = progressStore;
		_logger = logger;
	}

	public IReadOnlyList<DifficultyLevel> Difficulties { get; } = Enum.GetValues<DifficultyLevel>();
	public bool CanStartGeneration =>
		!IsGenerating && !IsInitializing && _initialized && (!RecoveryRequired || _unrecoverableSessionKey is not null);

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (IsInitializing)
			return;
		if (_sessionStore.Current?.Summary is not null)
			_sessionStore.Clear();
		if (_sessionStore.Current is not null && _sessionStore.ActiveRecord is not null)
		{
			_initialized = true;
			HasActiveGame = true;
			RecoveryRequired = false;
			StatusMessage = _sessionStore.IsPersisted
				? "Sua partida está salva e pode ser retomada."
				: "A partida está somente na memória; o salvamento será tentado novamente na próxima alteração.";
			OnPropertyChanged(nameof(CanStartGeneration));
			return;
		}

		IsInitializing = true;
		_initialized = false;
		HasLoadFailure = false;
		OnPropertyChanged(nameof(CanStartGeneration));
		try
		{
			var loaded = await _loadActiveGame.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				switch (loaded.Status)
				{
					case LoadActiveGameStatus.Loaded when loaded.Session is not null && loaded.Record is not null:
						_sessionStore.Start(loaded.Session, loaded.Record, _progressStore, isPersisted: true);
						HasActiveGame = true;
						RecoveryRequired = false;
						_unrecoverableSessionKey = null;
						HasLoadFailure = false;
						StatusMessage = "Uma partida salva está pronta para continuar.";
						break;
					case LoadActiveGameStatus.RecoveryRequired:
						_sessionStore.Clear();
						HasActiveGame = false;
						RecoveryRequired = true;
						_unrecoverableSessionKey = loaded.RecoverySessionKey;
						HasLoadFailure = _unrecoverableSessionKey is null;
						StatusMessage = "Não foi possível ler a partida salva. Confirme antes de descartá-la e iniciar outra.";
						break;
					default:
						_sessionStore.Clear();
						HasActiveGame = false;
						RecoveryRequired = false;
						_unrecoverableSessionKey = null;
						HasLoadFailure = false;
						StatusMessage = "Nenhuma partida está pronta para continuar. Você pode iniciar uma nova.";
						break;
				}
				_initialized = !HasLoadFailure;
				OnPropertyChanged(nameof(CanStartGeneration));
			});
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			return;
		}
		catch (Exception exception)
		{
			var correlationId = Guid.NewGuid().ToString("N");
			_logger.LogError(exception, "Loading the active game progress failed (CorrelationId {CorrelationId})", correlationId);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HasActiveGame = false;
				RecoveryRequired = false;
				_unrecoverableSessionKey = null;
				HasLoadFailure = true;
				StatusMessage = $"Não foi possível verificar a partida salva. Tente novamente antes de iniciar outra. Código: {correlationId}";
			});
		}
		finally
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				IsInitializing = false;
				OnPropertyChanged(nameof(CanStartGeneration));
			});
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task RetryLoadAsync(CancellationToken cancellationToken) => InitializeAsync(cancellationToken);

	[RelayCommand(AllowConcurrentExecutions = false)]
	private async Task ResumeActiveGameAsync()
	{
		try
		{
			if (_sessionStore.Current is null)
				await InitializeAsync();
			if (_sessionStore.Current is not null)
				await Shell.Current.GoToAsync(nameof(GamePage));
		}
		catch (Exception exception)
		{
			ReportFailure(exception, "Resuming the active game failed");
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private async Task GeneratePuzzleAsync(CancellationToken commandToken)
	{
		if (!CanStartGeneration || IsGenerating)
			return;
		if (!await ConfirmReplacementAsync())
			return;

		using var generationCancellation = CancellationTokenSource.CreateLinkedTokenSource(commandToken);
		_generationCancellation = generationCancellation;
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			IsGenerating = true;
			HasRetryableFailure = false;
			StatusMessage = "Gerando um puzzle. Você pode cancelar quando quiser.";
		});

		try
		{
			var request = new PuzzleGenerationRequest(
				SelectedDifficulty,
				new GenerationBudget(maxAttempts: 8, timeLimit: TimeSpan.FromSeconds(30)));
			var result = await _generatePuzzle.ExecuteAsync(request, generationCancellation.Token).ConfigureAwait(false);
			generationCancellation.Token.ThrowIfCancellationRequested();

			if (result.IsSuccess && result.GeneratedPuzzle is not null)
			{
				var replacement = await PrepareReplacementAsync(generationCancellation.Token).ConfigureAwait(false);
				if (!replacement.Allowed)
					return;

				var created = replacement.SessionKey is null
					? await _createGameProgress.ExecuteAsync(result.GeneratedPuzzle, generationCancellation.Token).ConfigureAwait(false)
					: await _createGameProgress.ReplaceActiveAsync(result.GeneratedPuzzle, replacement.SessionKey, generationCancellation.Token).ConfigureAwait(false);
				if (replacement.SessionKey is not null && !created.Persistence.IsSaved)
				{
					await MainThread.InvokeOnMainThreadAsync(() =>
					{
						HasRetryableFailure = true;
						StatusMessage = "Não foi possível salvar a nova partida; a partida anterior foi mantida.";
					});
					return;
				}

				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					_sessionStore.Clear();
					_sessionStore.Start(created.Session, created.Record, _progressStore, created.IsPersisted);
					HasActiveGame = true;
					RecoveryRequired = false;
					_unrecoverableSessionKey = null;
					HasLoadFailure = false;
					_initialized = true;
					StatusMessage = created.IsPersisted
						? string.Empty
						: "O primeiro salvamento falhou. A partida continua na memória; tente salvar novamente.";
					await Shell.Current.GoToAsync(nameof(GamePage));
				});
			}
			else
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					HasRetryableFailure = true;
					StatusMessage = "Não foi possível gerar esse puzzle. Tente novamente ou escolha outra dificuldade.";
				});
			}
		}
		catch (OperationCanceledException) when (generationCancellation.IsCancellationRequested)
		{
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HasRetryableFailure = true;
				StatusMessage = "Geração cancelada. Você pode tentar novamente.";
			});
		}
		catch (Exception exception)
		{
			ReportFailure(exception, "Puzzle generation or initial persistence failed");
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HasRetryableFailure = true;
				StatusMessage = "Ocorreu um erro ao gerar ou salvar a partida. Tente novamente.";
			});
		}
		finally
		{
			_generationCancellation = null;
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				IsGenerating = false;
				OnPropertyChanged(nameof(CanStartGeneration));
			});
		}
	}

	private async Task<bool> ConfirmReplacementAsync()
	{
		if (HasActiveGame)
		{
			return await Shell.Current.DisplayAlertAsync(
				"Substituir partida?",
				"A partida atual só será abandonada se o novo jogo for gerado e salvo.",
				"Substituir",
				"Cancelar");
		}
		if (RecoveryRequired)
		{
			return await Shell.Current.DisplayAlertAsync(
				"Descartar partida inválida?",
				"O salvamento só será abandonado se o novo jogo for gerado e salvo.",
				"Descartar e continuar",
				"Cancelar");
		}
		return true;
	}

	private async Task<(bool Allowed, string? SessionKey)> PrepareReplacementAsync(CancellationToken cancellationToken)
	{
		if (HasActiveGame)
		{
			if (_sessionStore.Commands is not { } commands || _sessionStore.Current is null)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					StatusMessage = "Não foi possível preservar a partida atual. Tente verificar o armazenamento novamente.");
				return (false, null);
			}

			var saved = await commands.PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
			if (!saved.IsConfirmed)
			{
				await MainThread.InvokeOnMainThreadAsync(() =>
					StatusMessage = "Não foi possível confirmar o salvamento da partida atual; ela foi mantida.");
				return (false, null);
			}
			return (true, commands.ActiveRecord.SessionId.ToString("D"));
		}
		if (RecoveryRequired)
		{
			if (_unrecoverableSessionKey is null)
				return (false, null);
			return (true, _unrecoverableSessionKey);
		}
		return (true, null);
	}
	[RelayCommand]
	private void CancelGeneration() => _generationCancellation?.Cancel();

	[RelayCommand]
	private async Task OpenStatisticsAsync()
	{
		try
		{
			await Shell.Current.GoToAsync(nameof(ProgressionStatisticsPage));
		}
		catch (Exception exception)
		{
			ReportFailure(exception, "Opening progression statistics failed");
		}
	}

	[RelayCommand]
	private async Task OpenThemeSettingsAsync()
	{
		try
		{
			await Shell.Current.GoToAsync(nameof(ThemeSettingsPage));
		}
		catch (Exception exception)
		{
			ReportFailure(exception, "Opening theme settings failed");
		}
	}

	private void ReportFailure(Exception exception, string operation)
	{
		var correlationId = Guid.NewGuid().ToString("N");
		_logger.LogError(exception, "{Operation} failed (CorrelationId {CorrelationId})", operation, correlationId);
		MainThread.BeginInvokeOnMainThread(() =>
		{
			HasRetryableFailure = true;
			StatusMessage = $"Ocorreu um erro inesperado. Tente novamente. Código: {correlationId}";
		});
	}

	partial void OnIsGeneratingChanged(bool value) => OnPropertyChanged(nameof(CanStartGeneration));
	partial void OnIsInitializingChanged(bool value) => OnPropertyChanged(nameof(CanStartGeneration));
}
