using CageLogic.Application.GameSessions;

namespace CageLogic.Application.Progression;

public sealed record CompleteGameProgressResult(
	GameSessionCompletion Completion,
	GameProgressRecord? Record,
	GameProgressWriteResult? Persistence);

/// <summary>Completes a validated solution and atomically converts its active row to a terminal record.</summary>
public sealed class CompleteGameProgressUseCase
{
	private readonly IGameProgressStore _store;
	private readonly TimeProvider _timeProvider;

	public CompleteGameProgressUseCase(IGameProgressStore store, TimeProvider? timeProvider = null)
	{
		ArgumentNullException.ThrowIfNull(store);
		_store = store;
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	public async Task<CompleteGameProgressResult> ExecuteAsync(
		GameSession session,
		GameProgressRecord activeRecord,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(activeRecord);
		cancellationToken.ThrowIfCancellationRequested();
		if (activeRecord.Status != GameProgressStatus.Active)
			throw new ArgumentException("Only an active game can be completed.", nameof(activeRecord));

		var completion = session.TryComplete();
		if (!completion.IsCompleted)
			return new CompleteGameProgressResult(completion, null, null);

		var completedRecord = activeRecord with
		{
			Status = GameProgressStatus.Completed,
			CompletedAtUtc = _timeProvider.GetUtcNow(),
			AbandonedAtUtc = null,
			ActiveElapsedTicks = session.ActiveElapsed.Ticks,
			ErrorCount = session.ErrorCount,
			DisplayedHintLevelCount = session.DisplayedHintLevelCount
		};
		var persisted = await _store.CompleteAsync(completedRecord, CancellationToken.None).ConfigureAwait(false);
		return new CompleteGameProgressResult(completion, completedRecord, persisted);
	}
}
