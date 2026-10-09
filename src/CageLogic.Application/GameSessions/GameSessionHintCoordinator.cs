using System.Diagnostics;
using CageLogic.Application.Hints;
using CageLogic.Domain.Board;

namespace CageLogic.Application.GameSessions;

/// <summary>Owns asynchronous, single-flight hint progression bound to immutable board revisions.</summary>
public sealed class GameSessionHintCoordinator
{
	private readonly object _sync = new();
	private readonly HintPuzzleContext _puzzleContext;
	private readonly Func<HintBoardSnapshot> _getCurrentBoard;
	private readonly Func<HintRequest, CancellationToken, Task<HintResult>> _execute;
	private readonly HashSet<(long Revision, HintLevel Level)> _countedLevels = [];
	private CancellationTokenSource? _cancellation;
	private Task<HintResult?>? _currentTask;
	private HintResult? _currentHint;
	private int _displayedHintLevelCount;
	private int _nextLevel = 1;

	public GameSessionHintCoordinator(
		HintPuzzleContext puzzleContext,
		Func<HintBoardSnapshot> getCurrentBoard,
		GetHintUseCase? getHintUseCase = null,
		Func<HintRequest, CancellationToken, Task<HintResult>>? execute = null,
		int initialDisplayedHintLevelCount = 0)
	{
		ArgumentNullException.ThrowIfNull(puzzleContext);
		ArgumentNullException.ThrowIfNull(getCurrentBoard);
		if (initialDisplayedHintLevelCount < 0)
			throw new ArgumentOutOfRangeException(nameof(initialDisplayedHintLevelCount));
		_puzzleContext = puzzleContext;
		_getCurrentBoard = getCurrentBoard;
		_displayedHintLevelCount = initialDisplayedHintLevelCount;
		var useCase = getHintUseCase ?? new GetHintUseCase();
		_execute = execute ?? useCase.ExecuteAsync;
	}

	public event EventHandler<GameSessionHintStateChangedEventArgs>? StateChanged;

	public HintResult? CurrentHint { get { lock (_sync) return _currentHint; } }
	public int DisplayedHintLevelCount { get { lock (_sync) return _displayedHintLevelCount; } }
	public bool IsPending { get { lock (_sync) return _currentTask is { IsCompleted: false }; } }

	public Task<HintResult?> RequestNextHintAsync(CancellationToken cancellationToken = default)
	{
		GameSessionHintStateChangedEventArgs changed;
		Task<HintResult?> task;
		HintRequest? requestToExecute = null;
		CancellationTokenSource? owner = null;
		CancellationToken executionToken = default;
		TaskCompletionSource<HintResult?>? completion = null;
		lock (_sync)
		{
			if (_currentTask is { IsCompleted: false })
				return _currentTask;

			var current = _getCurrentBoard();
			var level = (HintLevel)Math.Clamp(_nextLevel, 1, 3);
			_currentHint = null;
			if (!current.IsValid)
			{
				_currentHint = new HintResult(current.Revision, HintStatus.InconsistentState, level: level);
				_currentTask = Task.FromResult<HintResult?>(_currentHint);
				task = _currentTask;
			}
			else
			{
				var request = new HintRequest(_puzzleContext, current.Board, level, current.Revision);
				_cancellation?.Dispose();
				_cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				owner = _cancellation;
				executionToken = owner.Token;
				completion = new TaskCompletionSource<HintResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
				requestToExecute = request;
				_currentTask = completion.Task;
				task = completion.Task;
			}

			changed = CreateStateChangedArgs(current.Revision, analysisFailed: false);
		}

		NotifyStateChanged(changed);
		if (requestToExecute is not null && owner is not null && completion is not null)
			_ = ExecuteAsync(requestToExecute, owner, executionToken, completion);
		return task;
	}

	public Task<HintResult?> WaitForHintAsync()
	{
		lock (_sync)
			return _currentTask ?? Task.FromResult(_currentHint);
	}

	/// <summary>Invalidates analyses made stale by a value revision and restarts pending work at level one.</summary>
	public void OnBoardValuesChanged()
	{
		var current = _getCurrentBoard();
		CancellationTokenSource? cancellation;
		bool restart;
		GameSessionHintStateChangedEventArgs changed;
		lock (_sync)
		{
			restart = _currentTask is { IsCompleted: false };
			cancellation = _cancellation;
			_cancellation = null;
			_currentTask = null;
			_currentHint = null;
			_nextLevel = 1;
			changed = CreateStateChangedArgs(current.Revision, analysisFailed: false);
		}

		cancellation?.Cancel();
		cancellation?.Dispose();
		NotifyStateChanged(changed);
		if (restart)
			_ = ObserveRefreshAsync(RequestNextHintAsync());
	}

	private async Task ExecuteAsync(
		HintRequest request,
		CancellationTokenSource owner,
		CancellationToken token,
		TaskCompletionSource<HintResult?> completion)
	{
		HintResult? accepted = null;
		Exception? failure = null;
		GameSessionHintStateChangedEventArgs? changed = null;
		try
		{
			var result = await _execute(request, token).ConfigureAwait(false);
			lock (_sync)
			{
				var current = _getCurrentBoard();
				if (!token.IsCancellationRequested &&
					request.BoardRevision == current.Revision &&
					result.BoardRevision == current.Revision &&
					ReferenceEquals(_cancellation, owner))
				{
					_currentHint = result;
					if (result.Status == HintStatus.Available && _countedLevels.Add((request.BoardRevision, request.Level)))
					{
						_displayedHintLevelCount++;
						_nextLevel = Math.Min(3, (int)request.Level + 1);
					}
					accepted = result;
					changed = CreateStateChangedArgs(current.Revision, analysisFailed: false);
				}
				else if (!token.IsCancellationRequested &&
					request.BoardRevision == current.Revision &&
					ReferenceEquals(_cancellation, owner) &&
					result.BoardRevision != current.Revision)
				{
					failure = new InvalidOperationException("The hint result does not match the current board revision.");
					changed = CreateStateChangedArgs(current.Revision, analysisFailed: true);
				}
			}
		}
		catch (OperationCanceledException) when (token.IsCancellationRequested)
		{
			// Cancellation and stale-result discard are normal session transitions.
		}
		catch (Exception exception)
		{
			failure = exception;
			lock (_sync)
			{
				var current = _getCurrentBoard();
				if (ReferenceEquals(_cancellation, owner) && request.BoardRevision == current.Revision)
					changed = CreateStateChangedArgs(current.Revision, analysisFailed: true);
			}
		}
		finally
		{
			lock (_sync)
			{
				if (ReferenceEquals(_cancellation, owner))
				{
					_cancellation.Dispose();
					_cancellation = null;
					_currentTask = null;
				}
			}
		}

		// Resolve the request before notifying observers so a faulty subscriber can never strand its caller.
		if (accepted is not null && _getCurrentBoard().Revision != accepted.BoardRevision)
			accepted = null;

		if (failure is null)
			completion.TrySetResult(accepted);
		else
			completion.TrySetException(failure);

		if (changed is not null)
			NotifyStateChanged(changed);
	}

	private static async Task ObserveRefreshAsync(Task<HintResult?> refreshedTask)
	{
		try
		{
			await refreshedTask.ConfigureAwait(false);
		}
		catch
		{
			// The coordinator publishes a player-safe failure state before completing this internal refresh task.
		}
	}

	private GameSessionHintStateChangedEventArgs CreateStateChangedArgs(long revision, bool analysisFailed) =>
		new(_currentHint, _displayedHintLevelCount, revision, analysisFailed);

	private void NotifyStateChanged(GameSessionHintStateChangedEventArgs eventArgs)
	{
		var handlers = StateChanged;
		if (handlers is null)
			return;

		foreach (EventHandler<GameSessionHintStateChangedEventArgs> handler in handlers.GetInvocationList())
		{
			try
			{
				handler(this, eventArgs);
			}
			catch (Exception exception)
			{
				Trace.WriteLine($"Hint state observer failed: {exception.GetType().Name}");
			}
		}
	}
}

public sealed record HintBoardSnapshot(SudokuBoard Board, long Revision, bool IsValid);

public sealed class GameSessionHintStateChangedEventArgs(
	HintResult? hint,
	int displayedHintLevelCount,
	long boardRevision,
	bool analysisFailed) : EventArgs
{
	public HintResult? Hint { get; } = hint;
	public int DisplayedHintLevelCount { get; } = displayedHintLevelCount;
	public long BoardRevision { get; } = boardRevision;
	public bool AnalysisFailed { get; } = analysisFailed;
}
