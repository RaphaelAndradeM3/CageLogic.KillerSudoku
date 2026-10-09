namespace CageLogic.Application.GameSessions;

/// <summary>Accumulates only active foreground time using TimeProvider's monotonic clock.</summary>
public sealed class ActiveGameTimer
{
	private readonly TimeProvider _timeProvider;
	private readonly object _sync = new();
	private TimeSpan _accumulated;
	private long? _activeSince;
	private bool _isPaused = true;
	private bool _hasStarted;

	public ActiveGameTimer(TimeProvider? timeProvider = null)
	{
		_timeProvider = timeProvider ?? TimeProvider.System;
	}

	/// <summary>Starts elapsed time when the player first reaches the playable board.</summary>
	public bool Start()
	{
		lock (_sync)
		{
			if (_hasStarted)
				return false;

			_hasStarted = true;
			_isPaused = false;
			_activeSince = _timeProvider.GetTimestamp();
			return true;
		}
	}

	public TimeSpan Elapsed
	{
		get
		{
			lock (_sync)
				return _activeSince is { } start
					? _accumulated + _timeProvider.GetElapsedTime(start, _timeProvider.GetTimestamp())
					: _accumulated;
		}
	}

	public bool IsPaused
	{
		get
		{
			lock (_sync)
				return _isPaused;
		}
	}

	public void Pause()
	{
		lock (_sync)
		{
			if (_isPaused)
				return;

			if (_activeSince is { } start)
				_accumulated += _timeProvider.GetElapsedTime(start, _timeProvider.GetTimestamp());
			_activeSince = null;
			_isPaused = true;
		}
	}

	public void Resume()
	{
		lock (_sync)
		{
			if (!_hasStarted)
			{
				_hasStarted = true;
				_activeSince = _timeProvider.GetTimestamp();
				_isPaused = false;
				return;
			}

			if (!_isPaused)
				return;

			_activeSince = _timeProvider.GetTimestamp();
			_isPaused = false;
		}
	}
}
