namespace CageLogic.Application.Progression;

/// <summary>Calculates progression totals directly from durable game records.</summary>
public sealed class GetProgressionStatisticsUseCase(IGameProgressStore store)
{
	public async Task<ProgressionStatistics> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var records = await store.GetRecordsAsync(cancellationToken).ConfigureAwait(false);
		var uniqueRecords = records.DistinctBy(record => record.SessionId).ToArray();
		var completed = uniqueRecords.Where(record => record.Status == GameProgressStatus.Completed).ToArray();
		TimeSpan? average = null;
		TimeSpan? best = null;
		if (completed.Length > 0)
		{
			var averageTicks = completed.Average(record => (decimal)record.ActiveElapsedTicks);
			average = TimeSpan.FromTicks(decimal.ToInt64(decimal.Round(averageTicks, 0, MidpointRounding.AwayFromZero)));
			best = TimeSpan.FromTicks(completed.Min(record => record.ActiveElapsedTicks));
		}

		return new ProgressionStatistics(
			uniqueRecords.Length,
			completed.Length,
			average,
			best,
			uniqueRecords.Sum(record => (long)record.ErrorCount),
			uniqueRecords.Sum(record => (long)record.DisplayedHintLevelCount));
	}
}
