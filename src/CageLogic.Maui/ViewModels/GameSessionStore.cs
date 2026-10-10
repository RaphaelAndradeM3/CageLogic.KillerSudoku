using CageLogic.Application.GameSessions;
using CageLogic.Application.Progression;

namespace CageLogic.Maui.ViewModels;

/// <summary>Holds the active session and its serialized persistence queue while Shell navigates.</summary>
public sealed class GameSessionStore
{
	public GameSession? Current { get; private set; }
	public GameProgressRecord? ActiveRecord { get; private set; }
	public GameProgressCommandQueue? Commands { get; private set; }
	public bool IsPersisted => Commands?.IsPersisted == true;

	public void Start(GameSession session, GameProgressRecord record, IGameProgressStore progressStore, bool isPersisted)
	{
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(record);
		ArgumentNullException.ThrowIfNull(progressStore);
		Current = session;
		ActiveRecord = record;
		Commands = new GameProgressCommandQueue(session, record, progressStore, wasPersisted: isPersisted);
	}

	public void UpdateRecord(GameProgressRecord record)
	{
		ArgumentNullException.ThrowIfNull(record);
		ActiveRecord = record;
	}

	public async Task<GameProgressCommandResult?> PauseAndPersistAsync(CancellationToken cancellationToken = default)
	{
		var session = Current;
		var commands = Commands;
		if (session is null || commands is null || session.Summary is not null)
			return null;

		session.PauseForBackground();
		return await commands.PersistCurrentAsync(cancellationToken).ConfigureAwait(false);
	}

	public void Clear()
	{
		Current = null;
		ActiveRecord = null;
		Commands = null;
	}
}
