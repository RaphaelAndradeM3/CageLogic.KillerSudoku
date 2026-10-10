using CageLogic.Application.Progression;

namespace CageLogic.Application.GameSessions;

/// <summary>Serializes session edits and confirms them only after the corresponding snapshot commit.</summary>
public sealed class GameProgressCommandQueue
{
	private readonly GameSession _session;
	private readonly IGameProgressStore _store;
	private readonly GameSessionPersistenceMapper _mapper;
	private readonly GameSessionCommandQueue _commands;
	private GameProgressRecord _activeRecord;
	private bool _wasPersisted;

	public GameProgressCommandQueue(
		GameSession session,
		GameProgressRecord activeRecord,
		IGameProgressStore store,
		GameSessionPersistenceMapper? mapper = null,
		GameSessionCommandQueue? commandQueue = null,
		bool wasPersisted = true)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(activeRecord);
		ArgumentNullException.ThrowIfNull(store);
		if (activeRecord.Status != GameProgressStatus.Active)
			throw new ArgumentException("A progress command queue requires an active record.", nameof(activeRecord));
		_session = session;
		_activeRecord = activeRecord;
		_store = store;
		_mapper = mapper ?? new GameSessionPersistenceMapper();
		_commands = commandQueue ?? new GameSessionCommandQueue();
		_wasPersisted = wasPersisted;
	}

	public GameProgressRecord ActiveRecord => _activeRecord;
	public bool IsPersisted => _wasPersisted;

	/// <summary>Runs the mutation and its snapshot save as one ordered, unambiguous command.</summary>
	public Task<GameProgressCommandResult> ExecuteAsync(
		Func<bool> mutation,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(mutation);
		return _commands.ExecuteAsync(async () =>
		{
			var applied = mutation();
			if (!applied)
				return new GameProgressCommandResult(false, null);

			var persistence = await PersistSnapshotAsync().ConfigureAwait(false);
			return new GameProgressCommandResult(true, persistence);
		}, cancellationToken);
	}

	/// <summary>Persists a state change that the session published outside a board command, such as a displayed hint.</summary>
	public Task<GameProgressCommandResult> PersistCurrentAsync(CancellationToken cancellationToken = default) =>
		_commands.ExecuteAsync(async () =>
			new GameProgressCommandResult(true, await PersistSnapshotAsync().ConfigureAwait(false)),
			cancellationToken);

	/// <summary>Runs a terminal or lifecycle operation in the same order as edits and snapshot writes.</summary>
	public Task<TResult> ExecuteExclusiveAsync<TResult>(
		Func<Task<TResult>> operation,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(operation);
		return _commands.ExecuteAsync(operation, cancellationToken);
	}

	private async Task<GameProgressWriteResult> PersistSnapshotAsync()
	{
		var snapshot = _mapper.Capture(_session);
		var record = _activeRecord with
		{
			ActiveElapsedTicks = snapshot.ActiveElapsedTicks,
			ErrorCount = snapshot.ErrorCount,
			DisplayedHintLevelCount = snapshot.DisplayedHintLevelCount
		};
		var saved = new SavedGameSession(record, snapshot.Version, _mapper.Serialize(snapshot));
		var result = _wasPersisted
			? await _store.SaveAsync(saved, CancellationToken.None).ConfigureAwait(false)
			: await _store.CreateAsync(saved, CancellationToken.None).ConfigureAwait(false);
		if (result.IsSaved)
		{
			_activeRecord = record;
			_wasPersisted = true;
		}
		return result;
	}
}

public sealed record GameProgressCommandResult(bool Applied, GameProgressWriteResult? Persistence)
{
	public bool IsConfirmed => Applied && Persistence?.IsSaved == true;
}
