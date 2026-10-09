using CageLogic.Application.Difficulty;
using CageLogic.Application.Progression;

namespace CageLogic.Application.Tests.Progression;

public sealed class GetProgressionStatisticsUseCaseTests
{
	[Test]
	public async Task EmptyHistory_ReturnsEmptyCountsAndNoTimes()
	{
		var result = await new GetProgressionStatisticsUseCase(new FakeProgressStore([])).ExecuteAsync();

		Assert.That(result.StartedCount, Is.Zero);
		Assert.That(result.CompletedCount, Is.Zero);
		Assert.That(result.AverageCompletedTime, Is.Null);
		Assert.That(result.BestCompletedTime, Is.Null);
		Assert.That(result.TotalErrors, Is.Zero);
		Assert.That(result.TotalHints, Is.Zero);
	}

	[Test]
	public async Task AggregatesCompletedActiveAndAbandonedRecordsUsingCompletedTimesOnly()
	{
		var start = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
		var records = new[]
		{
			Record(Guid.NewGuid(), GameProgressStatus.Completed, start, start.AddMinutes(2), null, 3, 2),
			Record(Guid.NewGuid(), GameProgressStatus.Completed, start.AddDays(1), start.AddDays(1).AddMinutes(5), null, 4, 1),
			Record(Guid.NewGuid(), GameProgressStatus.Active, start.AddDays(2), null, null, 5, 3),
			Record(Guid.NewGuid(), GameProgressStatus.Abandoned, start.AddDays(3), null, start.AddDays(3).AddMinutes(9), 7, 4)
		};

		var result = await new GetProgressionStatisticsUseCase(new FakeProgressStore(records)).ExecuteAsync();

		Assert.That(result.StartedCount, Is.EqualTo(4));
		Assert.That(result.CompletedCount, Is.EqualTo(2));
		Assert.That(result.AverageCompletedTime, Is.EqualTo(TimeSpan.FromMinutes(3.5)));
		Assert.That(result.BestCompletedTime, Is.EqualTo(TimeSpan.FromMinutes(2)));
		Assert.That(result.TotalErrors, Is.EqualTo(19));
		Assert.That(result.TotalHints, Is.EqualTo(10));
	}

	[Test]
	public async Task DuplicateSessionIds_AreCountedOnce()
	{
		var record = Record(
			Guid.NewGuid(), GameProgressStatus.Completed,
			new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero),
			new DateTimeOffset(2026, 10, 1, 12, 4, 0, TimeSpan.Zero), null, 2, 1);

		var result = await new GetProgressionStatisticsUseCase(new FakeProgressStore([record, record])).ExecuteAsync();

		Assert.That(result.StartedCount, Is.EqualTo(1));
		Assert.That(result.CompletedCount, Is.EqualTo(1));
		Assert.That(result.TotalErrors, Is.EqualTo(2));
		Assert.That(result.TotalHints, Is.EqualTo(1));
	}

	private static GameProgressRecord Record(
		Guid id,
		GameProgressStatus status,
		DateTimeOffset started,
		DateTimeOffset? completed,
		DateTimeOffset? abandoned,
		int errors,
		int hints)
	{
		var activeElapsed = completed.HasValue ? completed.Value - started : TimeSpan.FromMinutes(9);
		return new GameProgressRecord(
			id, status, started, completed, abandoned, DifficultyLevel.Easy, activeElapsed.Ticks, errors, hints);
	}

	private sealed class FakeProgressStore(IReadOnlyList<GameProgressRecord> records) : IGameProgressStore
	{
		public Task<GameProgressWriteResult> CreateAsync(SavedGameSession session, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressLoadResult> LoadActiveAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressLoadResult.NoActiveSession());

		public Task<GameProgressWriteResult> SaveAsync(SavedGameSession session, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressWriteResult> CompleteAsync(GameProgressRecord completedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressWriteResult> AbandonAsync(GameProgressRecord abandonedRecord, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<GameProgressWriteResult> AbandonUnrecoverableActiveAsync(Guid sessionId, DateTimeOffset abandonedAtUtc, CancellationToken cancellationToken = default) =>
			Task.FromResult(GameProgressWriteResult.Saved());

		public Task<IReadOnlyList<GameProgressRecord>> GetRecordsAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult(records);
	}
}
