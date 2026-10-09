using CageLogic.Application.GameSessions;

namespace CageLogic.Maui.Lifecycle;

/// <summary>Pauses elapsed game time on app deactivation and never resumes it implicitly.</summary>
public sealed class GameSessionLifecycleBehavior : IDisposable
{
	private Window? _window;
	private GameSession? _session;
	private Func<Task>? _persistOnPause;

	public void Attach(Window window, GameSession session, Func<Task> persistOnPause)
	{
		ArgumentNullException.ThrowIfNull(window);
		ArgumentNullException.ThrowIfNull(session);
		ArgumentNullException.ThrowIfNull(persistOnPause);
		if (ReferenceEquals(_window, window) && ReferenceEquals(_session, session) && ReferenceEquals(_persistOnPause, persistOnPause))
			return;
		Detach(pause: false);
		_window = window;
		_session = session;
		_persistOnPause = persistOnPause;
		_window.Deactivated += OnDeactivated;
		_window.Stopped += OnStopped;
		_window.Resumed += OnResumed;
	}

	public void Detach(bool pause)
	{
		if (pause)
			PauseAndPersist();
		if (_window is not null)
		{
			_window.Deactivated -= OnDeactivated;
			_window.Stopped -= OnStopped;
			_window.Resumed -= OnResumed;
		}
		_window = null;
		_session = null;
		_persistOnPause = null;
	}

	private void OnDeactivated(object? sender, EventArgs eventArgs) => PauseAndPersist();
	private void OnStopped(object? sender, EventArgs eventArgs) => PauseAndPersist();

	private void PauseAndPersist()
	{
		if (_session is null || _session.Summary is not null)
			return;
		_session.PauseForBackground();
		if (_persistOnPause is not null)
			_ = PersistSafelyAsync(_persistOnPause);
	}

	private static async Task PersistSafelyAsync(Func<Task> persist)
	{
		try
		{
			await persist().ConfigureAwait(false);
		}
		catch
		{
			// The view model reports a safe status when persistence fails; lifecycle callbacks cannot throw into the host.
		}
	}
	private void OnResumed(object? sender, EventArgs eventArgs)
	{
		// Returning to the foreground never resumes the active timer implicitly.
	}

	public void Dispose() => Detach(pause: true);
}
