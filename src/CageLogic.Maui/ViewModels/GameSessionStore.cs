using CageLogic.Application.GameSessions;

namespace CageLogic.Maui.ViewModels;

/// <summary>Holds the one active in-memory session while Shell navigates between pages.</summary>
public sealed class GameSessionStore
{
	public GameSession? Current { get; private set; }

	public void Start(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		Current = session;
	}

	public void Clear() => Current = null;
}
