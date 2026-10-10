namespace CageLogic.Application.Progression;

/// <summary>Read-only projection over all persisted game records.</summary>
public sealed record ProgressionStatistics(
	int StartedCount,
	int CompletedCount,
	TimeSpan? AverageCompletedTime,
	TimeSpan? BestCompletedTime,
	long TotalErrors,
	long TotalHints);
