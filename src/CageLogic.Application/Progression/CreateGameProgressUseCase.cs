using CageLogic.Application.Generation;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;

namespace CageLogic.Application.Progression;

/// <summary>Creates an in-memory game and persists its first recoverable snapshot.</summary>
public sealed class CreateGameProgressUseCase
{
	private readonly IGameProgressStore _store;
	private readonly GameSessionPersistenceMapper _mapper;
	private readonly TimeProvider _timeProvider;
	private readonly GetHintUseCase? _getHintUseCase;

	public CreateGameProgressUseCase(
		IGameProgressStore store,
		TimeProvider? timeProvider = null,
		GameSessionPersistenceMapper? mapper = null,
		GetHintUseCase? getHintUseCase = null)
	{
		ArgumentNullException.ThrowIfNull(store);
		_store = store;
		_timeProvider = timeProvider ?? TimeProvider.System;
		_mapper = mapper ?? new GameSessionPersistenceMapper();
		_getHintUseCase = getHintUseCase;
	}

	public async Task<CreateGameProgressResult> ExecuteAsync(
		GeneratedPuzzle generatedPuzzle,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(generatedPuzzle);
		cancellationToken.ThrowIfCancellationRequested();

		var session = new GameSession(generatedPuzzle, getHintUseCase: _getHintUseCase, timeProvider: _timeProvider);
		var record = new GameProgressRecord(
			Guid.NewGuid(),
			GameProgressStatus.Active,
			_timeProvider.GetUtcNow(),
			null,
			null,
			generatedPuzzle.RequestedDifficulty,
			0,
			0,
			0);
		var snapshot = _mapper.Capture(session);
		var saved = await _store.CreateAsync(
			new SavedGameSession(record, snapshot.Version, _mapper.Serialize(snapshot)),
			CancellationToken.None).ConfigureAwait(false);
		return new CreateGameProgressResult(session, record, saved);
	}
}

public sealed record CreateGameProgressResult(
	GameSession Session,
	GameProgressRecord Record,
	GameProgressWriteResult Persistence)
{
	public bool IsPersisted => Persistence.IsSaved;
}
