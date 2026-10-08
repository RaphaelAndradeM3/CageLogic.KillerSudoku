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
		Func<HintRequest, CancellationToken, Task<HintResult>>? execute = null)
	{
		ArgumentNullException.ThrowIfNull(puzzleContext);
		ArgumentNullException.ThrowIfNull(getCurrentBoard);
		_puzzleContext = puzzleContext;
		_getCurrentBoard = getCurrentBoard;
		var useCase = getHintUseCase ?? new GetHintUseCase();
		_execute = execute ?? useCase.ExecuteAsync;
	}

	public event EventHandler<GameSessionHintStateChangedEventArgs>? StateChanged;

	public HintResult? CurrentHint { get { lock (_sync) return _currentHint; } }
	public int DisplayedHintLevelCount { get { lock (_sync) return _displayedHintLevelCount; } }

	public Task<HintResult?> RequestNextHintAsync(CancellationToken cancellationToken = default)
	{
		GameSessionHintStateChangedEventArgs? changed = null;
		Task<HintResult?> task;
		lock (_sync)
		{
			if (_currentTask is { IsCompleted: false })
				return _currentTask;

			var current = _getCurrentBoard();
			var level = (HintLevel)Math.Clamp(_nextLevel, 1, 3);
			if (!current.IsValid)
			{
				_currentHint = new HintResult(current.Revision, HintStatus.InconsistentState, level: level);
				_currentTask = Task.FromResult<HintResult?>(_currentHint);
				changed = CreateStateChangedArgs();
				task = _currentTask;
			}
			else
			{
				var request = new HintRequest(_puzzleContext, current.Board, level, current.Revision);
				_cancellation?.Dispose();
				_cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				_currentHint = null;
				var owner = _cancellation;
				var completion = new TaskCompletionSource<HintResult?>(TaskCreationOptions.RunContinuationsAsynchronously);
				_currentTask = completion.Task;
				task = completion.Task;
				_ = ExecuteAsync(request, owner, owner.Token, completion);
			}
		}
		if (changed is not null)
			StateChanged?.Invoke(this, changed);
		return task;
	}

	public Task<HintResult?> WaitForHintAsync()
	{
		lock (_sync)
			return _currentTask ?? Task.FromResult(_currentHint);
	}

	/// <summary>Invalidates only analyses made stale by a value revision and restarts pending work at level one.</summary>
	public void OnBoardValuesChanged()
	{
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
			changed = CreateStateChangedArgs();
		}
		cancellation?.Cancel();
		cancellation?.Dispose();
		StateChanged?.Invoke(this, changed);
		if (restart)
			_ = RequestNextHintAsync();
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
				if (!token.IsCancellationRequested && request.BoardRevision == current.Revision && ReferenceEquals(_cancellation, owner))
				{
					_currentHint = result;
					if (result.Status == HintStatus.Available && _countedLevels.Add((request.BoardRevision, request.Level)))
					{
						_displayedHintLevelCount++;
						_nextLevel = Math.Min(3, (int)request.Level + 1);
					}
					accepted = result;
					changed = CreateStateChangedArgs();
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
		if (changed is not null)
			StateChanged?.Invoke(this, changed);
		if (failure is null)
			completion.TrySetResult(accepted);
		else
			completion.TrySetException(failure);
	}

	private GameSessionHintStateChangedEventArgs CreateStateChangedArgs() =>
		new(_currentHint, _displayedHintLevelCount);
}

public sealed record HintBoardSnapshot(SudokuBoard Board, long Revision, bool IsValid);

public sealed class GameSessionHintStateChangedEventArgs(HintResult? hint, int displayedHintLevelCount) : EventArgs
{
	public HintResult? Hint { get; } = hint;
	public int DisplayedHintLevelCount { get; } = displayedHintLevelCount;
}
