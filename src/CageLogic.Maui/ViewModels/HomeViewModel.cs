using CageLogic.Application.Difficulty;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Generation;
using CageLogic.Application.Hints;
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
	private readonly GetHintUseCase _getHintUseCase;
	private readonly ILogger<HomeViewModel> _logger;
	private CancellationTokenSource? _generationCancellation;

	[ObservableProperty]
	public partial DifficultyLevel SelectedDifficulty { get; set; } = DifficultyLevel.Easy;

	[ObservableProperty]
	public partial bool IsGenerating { get; set; }

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	[ObservableProperty]
	public partial bool HasRetryableFailure { get; set; }

	public HomeViewModel(
		GeneratePuzzleUseCase generatePuzzle,
		GameSessionStore sessionStore,
		GetHintUseCase getHintUseCase,
		ILogger<HomeViewModel> logger)
	{
		_generatePuzzle = generatePuzzle;
		_sessionStore = sessionStore;
		_getHintUseCase = getHintUseCase;
		_logger = logger;
	}

	public IReadOnlyList<DifficultyLevel> Difficulties { get; } = Enum.GetValues<DifficultyLevel>();

	public bool CanStartGeneration => !IsGenerating;

	[RelayCommand(AllowConcurrentExecutions = false)]
	private async Task GeneratePuzzleAsync(CancellationToken commandToken)
	{
		if (IsGenerating)
			return;

		using var generationCancellation = CancellationTokenSource.CreateLinkedTokenSource(commandToken);
		_generationCancellation = generationCancellation;
		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			IsGenerating = true;
			HasRetryableFailure = false;
			StatusMessage = "Gerando um puzzle. Você pode cancelar a qualquer momento.";
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
				var session = await Task.Run(
					() => new GameSession(result.GeneratedPuzzle, getHintUseCase: _getHintUseCase),
					generationCancellation.Token)
					.ConfigureAwait(false);
				generationCancellation.Token.ThrowIfCancellationRequested();
				await MainThread.InvokeOnMainThreadAsync(async () =>
				{
					generationCancellation.Token.ThrowIfCancellationRequested();
					_sessionStore.Start(session);
					StatusMessage = string.Empty;
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
			_logger.LogError(exception, "Puzzle generation failed for {Difficulty}", SelectedDifficulty);
			await MainThread.InvokeOnMainThreadAsync(() =>
			{
				HasRetryableFailure = true;
				StatusMessage = "Ocorreu um erro ao gerar o puzzle. Tente novamente.";
			});
		}
		finally
		{
			_generationCancellation = null;
			await MainThread.InvokeOnMainThreadAsync(() => IsGenerating = false);
		}
	}

	[RelayCommand]
	private void CancelGeneration()
	{
		_generationCancellation?.Cancel();
	}

	partial void OnIsGeneratingChanged(bool value)
	{
		OnPropertyChanged(nameof(CanStartGeneration));
	}
}
