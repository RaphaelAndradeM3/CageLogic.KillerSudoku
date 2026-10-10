using System.Globalization;
using CageLogic.Application.Progression;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace CageLogic.Maui.ViewModels;

public partial class ProgressionStatisticsViewModel : ObservableObject
{
	private readonly GetProgressionStatisticsUseCase _getStatistics;
	private readonly ILogger<ProgressionStatisticsViewModel> _logger;

	[ObservableProperty]
	public partial ProgressionStatistics Statistics { get; set; } = new(0, 0, null, null, 0, 0);

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	[ObservableProperty]
	public partial string StatusMessage { get; set; } = string.Empty;

	public ProgressionStatisticsViewModel(
		GetProgressionStatisticsUseCase getStatistics,
		ILogger<ProgressionStatisticsViewModel> logger)
	{
		_getStatistics = getStatistics;
		_logger = logger;
	}

	public bool HasNoCompletedGames => Statistics.CompletedCount == 0;
	public string AverageCompletedTimeText => FormatDuration(Statistics.AverageCompletedTime);
	public string BestCompletedTimeText => FormatDuration(Statistics.BestCompletedTime);

	public async Task LoadAsync(CancellationToken cancellationToken = default)
	{
		if (IsLoading)
			return;
		IsLoading = true;
		StatusMessage = string.Empty;
		try
		{
			var statistics = await _getStatistics.ExecuteAsync(cancellationToken).ConfigureAwait(false);
			await MainThread.InvokeOnMainThreadAsync(() => Statistics = statistics);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			// Navigation can cancel a refresh without changing the current projection.
		}
		catch (Exception exception)
		{
			var correlationId = Guid.NewGuid().ToString("N");
			_logger.LogError(exception, "Loading progression statistics failed (CorrelationId {CorrelationId})", correlationId);
			await MainThread.InvokeOnMainThreadAsync(() =>
				StatusMessage = $"Não foi possível carregar as estatísticas. Tente novamente. Código: {correlationId}");
		}
		finally
		{
			await MainThread.InvokeOnMainThreadAsync(() => IsLoading = false);
		}
	}

	[RelayCommand(AllowConcurrentExecutions = false)]
	private Task RefreshAsync(CancellationToken cancellationToken) => LoadAsync(cancellationToken);

	partial void OnStatisticsChanged(ProgressionStatistics value)
	{
		OnPropertyChanged(nameof(HasNoCompletedGames));
		OnPropertyChanged(nameof(AverageCompletedTimeText));
		OnPropertyChanged(nameof(BestCompletedTimeText));
	}

	private static string FormatDuration(TimeSpan? duration) => duration is null
		? "—"
		: string.Create(CultureInfo.CurrentCulture, $"{(int)duration.Value.TotalHours:00}:{duration.Value.Minutes:00}:{duration.Value.Seconds:00}");
}
