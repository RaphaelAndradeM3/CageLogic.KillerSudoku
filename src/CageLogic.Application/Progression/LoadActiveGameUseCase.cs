using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;

namespace CageLogic.Application.Progression;

public enum LoadActiveGameStatus
{
	Loaded,
	NoActiveSession,
	RecoveryRequired
}

public sealed record LoadActiveGameResult(
	LoadActiveGameStatus Status,
	GameSession? Session = null,
	GameProgressRecord? Record = null,
	string? RecoveryReason = null);

/// <summary>Loads and validates the active snapshot before exposing a resumable session.</summary>
public sealed class LoadActiveGameUseCase(
	IGameProgressStore store,
	GameSessionPersistenceMapper? mapper = null,
	GetHintUseCase? getHintUseCase = null)
{
	private readonly GameSessionPersistenceMapper _mapper = mapper ?? new GameSessionPersistenceMapper();

	public async Task<LoadActiveGameResult> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		var loaded = await store.LoadActiveAsync(cancellationToken).ConfigureAwait(false);
		if (loaded.Status == GameProgressLoadStatus.NoActiveSession)
			return new LoadActiveGameResult(LoadActiveGameStatus.NoActiveSession);
		if (loaded.Status == GameProgressLoadStatus.RecoveryRequired)
			return new LoadActiveGameResult(LoadActiveGameStatus.RecoveryRequired, Record: loaded.Record, RecoveryReason: loaded.RecoveryReason);
		if (loaded.Session is null)
			return new LoadActiveGameResult(LoadActiveGameStatus.RecoveryRequired, RecoveryReason: "ActiveSnapshotMissing");

		try
		{
			var snapshot = _mapper.Deserialize(loaded.Session.SnapshotJson);
			if (snapshot.Version != loaded.Session.SnapshotVersion)
				return new LoadActiveGameResult(LoadActiveGameStatus.RecoveryRequired, Record: loaded.Session.Record, RecoveryReason: "SnapshotVersionMismatch");
			var session = _mapper.Restore(snapshot, getHintUseCase: getHintUseCase);
			return new LoadActiveGameResult(LoadActiveGameStatus.Loaded, session, loaded.Session.Record);
		}
		catch (InvalidDataException)
		{
			return new LoadActiveGameResult(LoadActiveGameStatus.RecoveryRequired, Record: loaded.Session.Record, RecoveryReason: "SnapshotInvalid");
		}
	}
}
