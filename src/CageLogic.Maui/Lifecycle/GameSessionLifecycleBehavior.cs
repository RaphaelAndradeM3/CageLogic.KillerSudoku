using CageLogic.Application.GameSessions;

namespace CageLogic.Maui.Lifecycle;

/// <summary>Pauses elapsed game time on app deactivation and never resumes it implicitly.</summary>
public sealed class GameSessionLifecycleBehavior : IDisposable
{
	private Window? _window;
	private GameSession? _session;

	public void Attach(Window window, GameSession session)
	{
		ArgumentNullException.ThrowIfNull(window);
		ArgumentNullException.ThrowIfNull(session);
		if (ReferenceEquals(_window, window) && ReferenceEquals(_session, session))
			return;
		Detach(pause: false);
		_window = window;
		_session = session;
		_window.Deactivated += OnDeactivated;
		_window.Stopped += OnStopped;
		_window.Resumed += OnResumed;
	}

	public void Detach(bool pause)
	{
		if (pause)
			_session?.Pause();
		if (_window is not null)
		{
			_window.Deactivated -= OnDeactivated;
			_window.Stopped -= OnStopped;
			_window.Resumed -= OnResumed;
		}
		_window = null;
		_session = null;
	}

	private void OnDeactivated(object? sender, EventArgs eventArgs) => _session?.PauseForBackground();
	private void OnStopped(object? sender, EventArgs eventArgs) => _session?.PauseForBackground();
	private void OnResumed(object? sender, EventArgs eventArgs)
	{
		// Returning to the foreground never resumes the active timer implicitly.
	}

	public void Dispose() => Detach(pause: true);
}
