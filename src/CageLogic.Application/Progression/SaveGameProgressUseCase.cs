using CageLogic.Application.GameSessions;

namespace CageLogic.Application.Progression;

/// <summary>Captures the latest complete session state and commits it as one active snapshot.</summary>
public sealed class SaveGameProgressUseCase
{
	private readonly IGameProgressStore _store;
	private readonly GameSessionPersistenceMapper _mapper;

	public SaveGameProgressUseCase(IGameProgressStore store, GameSessionPersistenceMapper? mapper = null)
	{
		ArgumentNullException.ThrowIfNull(store);
		_store = store;
		_mapper = mapper ?? new GameSessionPersistenceMapper();
	}

	public async Task<GameProgressWriteResult> ExecuteAsync(
		GameSession session,
		GameProgressRecord activeRecord,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(activeRecord);
		cancellationToken.ThrowIfCancellationRequested();
		if (activeRecord.Status != GameProgressStatus.Active)
			return GameProgressWriteResult.Failed("ActiveRecordRequired");

		var snapshot = _mapper.Capture(session);
		var updatedRecord = activeRecord with
		{
			ActiveElapsedTicks = snapshot.ActiveElapsedTicks,
			ErrorCount = snapshot.ErrorCount,
			DisplayedHintLevelCount = snapshot.DisplayedHintLevelCount
		};
		return await _store.SaveAsync(
			new SavedGameSession(updatedRecord, snapshot.Version, _mapper.Serialize(snapshot)),
			CancellationToken.None).ConfigureAwait(false);
	}
}
