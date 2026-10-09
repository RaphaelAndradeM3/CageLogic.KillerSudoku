using CageLogic.Application.GameSessions;

namespace CageLogic.Application.Progression;

public sealed record AbandonGameProgressResult(GameProgressRecord Record, GameProgressWriteResult Persistence);

/// <summary>Pauses active time and atomically marks an active game as abandoned.</summary>
public sealed class AbandonGameProgressUseCase
{
	private readonly IGameProgressStore _store;
	private readonly TimeProvider _timeProvider;

	public AbandonGameProgressUseCase(IGameProgressStore store, TimeProvider? timeProvider = null)
	{
		ArgumentNullException.ThrowIfNull(store);
		_store = store;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	public async Task<AbandonGameProgressResult> ExecuteAsync(
		GameSession session,
		GameProgressRecord activeRecord,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(activeRecord);
		cancellationToken.ThrowIfCancellationRequested();
		if (activeRecord.Status != GameProgressStatus.Active)
			throw new ArgumentException("Only an active game can be abandoned.", nameof(activeRecord));

		session.Pause();
		var abandonedRecord = activeRecord with
		{
			Status = GameProgressStatus.Abandoned,
			CompletedAtUtc = null,
			AbandonedAtUtc = _timeProvider.GetUtcNow(),
			ActiveElapsedTicks = session.ActiveElapsed.Ticks,
			ErrorCount = session.ErrorCount,
			DisplayedHintLevelCount = session.DisplayedHintLevelCount
		};
		var persisted = await _store.AbandonAsync(abandonedRecord, CancellationToken.None).ConfigureAwait(false);
		return new AbandonGameProgressResult(abandonedRecord, persisted);
	}
}
