using CageLogic.Application.Candidates;
using CageLogic.Application.Generation;
using CageLogic.Application.Hints;
using CageLogic.Application.Moves;
using CageLogic.Application.Solving;
using CageLogic.Application.Validation;
using CageLogic.Domain.Board;
using CageLogic.Domain.Candidates;
using CageLogic.Domain.Moves;
using CageLogic.Domain.Validation;

namespace CageLogic.Application.GameSessions;

/// <summary>Coordinates the mutable lifecycle around immutable board snapshots for one game.</summary>
public sealed class GameSession
{
	private readonly object _sync = new();
	private readonly GeneratedPuzzle _generatedPuzzle;
	private readonly ApplyMoveUseCase _applyMove;
	private readonly ValidateBoardUseCase _validateBoard;
	private readonly GetCandidatesUseCase _getCandidates;
	private readonly GameSessionHintCoordinator _hintCoordinator;
	private readonly SessionHistory _history = new();
	private readonly ActiveGameTimer _timer;
	private SudokuBoard _board;
	private CandidateNotes _notes = CandidateNotes.Empty;
	private CellPosition? _selectedPosition;
	private GameInputMode _inputMode = GameInputMode.Answer;
	private HashSet<CellPosition> _conflictingPositions = [];
	private bool _isBoardValid;
	private long _boardRevision;
	private int _errorCount;
	private int _displayedHintLevelCount;
	private HintResult? _hint;
	private bool _hintAnalysisFailed;
	private SessionSummary? _summary;

	public GameSession(
		GeneratedPuzzle generatedPuzzle,
		ApplyMoveUseCase? applyMove = null,
		ValidateBoardUseCase? validateBoard = null,
		GetCandidatesUseCase? getCandidates = null,
		GetHintUseCase? getHintUseCase = null,
		Func<HintRequest, CancellationToken, Task<HintResult>>? hintExecutor = null,
		TimeProvider? timeProvider = null,
		int initialDisplayedHintLevelCount = 0)
	{
		ArgumentNullException.ThrowIfNull(generatedPuzzle);
		if (initialDisplayedHintLevelCount < 0)
			throw new ArgumentOutOfRangeException(nameof(initialDisplayedHintLevelCount));
		_generatedPuzzle = generatedPuzzle;
		_applyMove = applyMove ?? new ApplyMoveUseCase();
		_validateBoard = validateBoard ?? new ValidateBoardUseCase();
		_getCandidates = getCandidates ?? new GetCandidatesUseCase();
		_timer = new ActiveGameTimer(timeProvider);
		_board = generatedPuzzle.Puzzle.CreateBoard();
		_displayedHintLevelCount = initialDisplayedHintLevelCount;
		UpdateValidation(_validateBoard.Execute(_board));
		var puzzleContext = new HintPuzzleContext(generatedPuzzle.Puzzle, SolutionMultiplicity.Unique, generatedPuzzle.Solution);
		_hintCoordinator = new GameSessionHintCoordinator(
			puzzleContext,
			GetHintBoardSnapshot,
			getHintUseCase,
			hintExecutor,
			initialDisplayedHintLevelCount);
		_hintCoordinator.StateChanged += OnHintCoordinatorStateChanged;
	}

	internal static GameSession Restore(
		GeneratedPuzzle generatedPuzzle,
		SudokuBoard board,
		CandidateNotes notes,
		SessionHistoryState history,
		TimeSpan activeElapsed,
		int errorCount,
		int displayedHintLevelCount,
		TimeProvider? timeProvider = null,
		GetHintUseCase? getHintUseCase = null)
	{
		ArgumentNullException.ThrowIfNull(board);
		ArgumentNullException.ThrowIfNull(notes);
		ArgumentNullException.ThrowIfNull(history);
		if (activeElapsed < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(activeElapsed));
		if (errorCount < 0)
			throw new ArgumentOutOfRangeException(nameof(errorCount));

		var session = new GameSession(
			generatedPuzzle,
			getHintUseCase: getHintUseCase,
			timeProvider: timeProvider,
			initialDisplayedHintLevelCount: displayedHintLevelCount);
		lock (session._sync)
		{
			session._board = board;
			session._notes = notes;
			session._history.RestoreState(history);
			session._errorCount = errorCount;
			session._timer.RestorePaused(activeElapsed);
			session.UpdateValidation(session._validateBoard.Execute(board));
		}
		return session;
	}

	internal GameSessionPersistenceState CapturePersistenceState()
	{
		lock (_sync)
			return new GameSessionPersistenceState(
				_generatedPuzzle,
				_board,
				_notes,
				_history.CaptureState(),
				_timer.Elapsed,
				_errorCount,
				_displayedHintLevelCount);
	}

	/// <summary>Raised after a state change. UI hosts should marshal their binding refresh to the UI thread.</summary>
	public event EventHandler? ViewStateChanged;

	public GameSessionViewState ViewState
	{
		get
		{
			lock (_sync)
				return CreateViewState();
		}
	}

	public SudokuBoard CurrentBoardSnapshot
	{
		get { lock (_sync) return _board; }
	}

	public long BoardRevision { get { lock (_sync) return _boardRevision; } }
	public int ErrorCount { get { lock (_sync) return _errorCount; } }
	public int DisplayedHintLevelCount { get { lock (_sync) return _displayedHintLevelCount; } }
	public bool IsHintPending => _hintCoordinator.IsPending;
	public bool HintAnalysisFailed { get { lock (_sync) return _hintAnalysisFailed; } }
	public bool CanUndo { get { lock (_sync) return _history.CanUndo; } }
	public bool CanRedo { get { lock (_sync) return _history.CanRedo; } }
	public GameInputMode InputMode { get { lock (_sync) return _inputMode; } }
	public TimeSpan ActiveElapsed => _timer.Elapsed;
	public bool IsPaused => _timer.IsPaused;
	public SessionSummary? Summary { get { lock (_sync) return _summary; } }

	public void SelectCell(CellPosition position)
	{
		lock (_sync)
			_selectedPosition = position;
		RaiseViewStateChanged();
	}

	public bool SetInputMode(GameInputMode mode)
	{
		if (!Enum.IsDefined(mode))
			throw new ArgumentOutOfRangeException(nameof(mode));
		lock (_sync)
		{
			if (_inputMode == mode)
				return false;
			_inputMode = mode;
		}
		RaiseViewStateChanged();
		return true;
	}

	/// <summary>Enters a note in Candidate mode, or validates and records an answer in Answer mode.</summary>
	public bool EnterDigit(int digit)
	{
		CellPosition position;
		GameInputMode mode;
		lock (_sync)
		{
			if (_summary is not null || !_selectedPosition.HasValue)
				return false;
			position = _selectedPosition.Value;
			mode = _inputMode;
		}

		return EnterDigit(position, mode, digit);
	}

	/// <summary>Applies a digit to the cell and mode captured when the player dispatched the intent.</summary>
	public bool EnterDigit(CellPosition position, GameInputMode mode, int digit)
	{
		if (digit is < 1 or > 9)
			return false;
		if (!Enum.IsDefined(mode))
			throw new ArgumentOutOfRangeException(nameof(mode));

		SudokuBoard beforeBoard;
		CandidateNotes beforeNotes;
		lock (_sync)
		{
			if (_summary is not null)
				return false;
			beforeBoard = _board;
			beforeNotes = _notes;
			var cell = beforeBoard.GetCell(position);
			if (cell.GivenValue.HasValue || (mode == GameInputMode.Candidate && cell.CurrentValue.HasValue))
				return false;
		}

		if (mode == GameInputMode.Candidate)
		{
			if (!beforeNotes.Toggle(position, digit, out var updatedNotes))
				return false;
			lock (_sync)
			{
				if (!ReferenceEquals(beforeBoard, _board) || !ReferenceEquals(beforeNotes, _notes) || _summary is not null)
					return false;
				_history.Record(_board, _notes, _board, updatedNotes);
				_notes = updatedNotes;
			}
			RaiseViewStateChanged();
			return true;
		}

		var result = _applyMove.Execute(beforeBoard, new Move(position, digit));
		if (!result.IsApplied)
			return false;
		var updatedAnswerNotes = beforeNotes.Clear(position, out var clearedNotes) ? clearedNotes : beforeNotes;
		var valueChanged = !SessionHistory.BoardsHaveSameValues(beforeBoard, result.Board);
		lock (_sync)
		{
			if (!ReferenceEquals(beforeBoard, _board) || !ReferenceEquals(beforeNotes, _notes) || _summary is not null)
				return false;
			if (!valueChanged && ReferenceEquals(updatedAnswerNotes, beforeNotes))
				return false;
			_history.Record(_board, _notes, result.Board, updatedAnswerNotes);
			_board = result.Board;
			_notes = updatedAnswerNotes;
			UpdateValidation(result.Validation);
			if (valueChanged)
			{
				_boardRevision++;
				if (_conflictingPositions.Contains(position))
					_errorCount++;
			}
		}
		if (valueChanged)
			OnBoardValuesChanged();
		else
			RaiseViewStateChanged();
		return true;
	}

	public bool ClearSelected()
	{
		CellPosition position;
		GameInputMode mode;
		lock (_sync)
		{
			if (_summary is not null || !_selectedPosition.HasValue)
				return false;
			position = _selectedPosition.Value;
			mode = _inputMode;
		}

		return ClearSelected(position, mode);
	}

	/// <summary>Clears the cell and mode captured when the player dispatched the intent.</summary>
	public bool ClearSelected(CellPosition position, GameInputMode mode)
	{
		if (!Enum.IsDefined(mode))
			throw new ArgumentOutOfRangeException(nameof(mode));

		SudokuBoard beforeBoard;
		CandidateNotes beforeNotes;
		lock (_sync)
		{
			if (_summary is not null)
				return false;
			beforeBoard = _board;
			beforeNotes = _notes;
			if (beforeBoard.GetCell(position).GivenValue.HasValue)
				return false;
		}

		if (mode == GameInputMode.Candidate)
		{
			if (!beforeNotes.Clear(position, out var cleared))
				return false;
			lock (_sync)
			{
				if (!ReferenceEquals(beforeBoard, _board) || !ReferenceEquals(beforeNotes, _notes) || _summary is not null)
					return false;
				_history.Record(_board, _notes, _board, cleared);
				_notes = cleared;
			}
			RaiseViewStateChanged();
			return true;
		}

		var result = _applyMove.Execute(beforeBoard, Move.Clear(position));
		if (!result.IsApplied)
			return false;
		var valueChanged = !SessionHistory.BoardsHaveSameValues(beforeBoard, result.Board);
		lock (_sync)
		{
			if (!ReferenceEquals(beforeBoard, _board) || !ReferenceEquals(beforeNotes, _notes) || _summary is not null || !valueChanged)
				return false;
			_history.Record(_board, _notes, result.Board, _notes);
			_board = result.Board;
			UpdateValidation(result.Validation);
			_boardRevision++;
		}
		OnBoardValuesChanged();
		return true;
	}

	/// <summary>Replaces all notes on empty editable cells as one undoable operation.</summary>
	public bool AutoFillCandidates()
	{
		SudokuBoard beforeBoard;
		CandidateNotes beforeNotes;
		lock (_sync)
		{
			if (_summary is not null)
				return false;
			beforeBoard = _board;
			beforeNotes = _notes;
		}

		IReadOnlyList<CandidateSet> candidateSets = _getCandidates.Execute(beforeBoard);
		var replacements = candidateSets
			.Where(set => !beforeBoard.GetCell(set.Position).GivenValue.HasValue && !beforeBoard.GetCell(set.Position).CurrentValue.HasValue)
			.Select(set => new KeyValuePair<CellPosition, IEnumerable<int>>(set.Position, set.Values));
		var nextNotes = CandidateNotes.Replace(replacements);
		lock (_sync)
		{
			if (!ReferenceEquals(beforeBoard, _board) || !ReferenceEquals(beforeNotes, _notes) || _summary is not null || beforeNotes.ContentEquals(nextNotes))
				return false;
			_history.Record(_board, _notes, _board, nextNotes);
			_notes = nextNotes;
		}
		RaiseViewStateChanged();
		return true;
	}

	public bool Undo() => RestoreHistory(undo: true);
	public bool Redo() => RestoreHistory(undo: false);

	public void Pause()
	{
		_timer.Pause();
		RaiseViewStateChanged();
	}

	/// <summary>Starts the active clock once the game page is ready for play.</summary>
	public void Start()
	{
		if (_timer.Start())
			RaiseViewStateChanged();
	}

	/// <summary>Pauses active play when the host sends the session to the background.</summary>
	public void PauseForBackground() => Pause();

	public void Resume()
	{
		lock (_sync)
		{
			if (_summary is not null)
				return;
			_timer.Resume();
		}
		RaiseViewStateChanged();
	}

	/// <summary>Requests the next progressive hint level. Concurrent requests share a single analysis.</summary>
	public Task<HintResult?> RequestNextHintAsync(CancellationToken cancellationToken = default) =>
		_hintCoordinator.RequestNextHintAsync(cancellationToken);

	public Task<HintResult?> WaitForHintAsync() => _hintCoordinator.WaitForHintAsync();

	public GameSessionCompletion TryComplete()
	{
		lock (_sync)
		{
			if (_summary is not null)
				return new GameSessionCompletion(SessionCompletionStatus.Completed, _summary);
			var validation = _validateBoard.Execute(_board);
			UpdateValidation(validation);
			if (!validation.IsValid)
				return new GameSessionCompletion(SessionCompletionStatus.ConflictingBoard, null);
			if (!validation.IsComplete)
				return new GameSessionCompletion(SessionCompletionStatus.Incomplete, null);
			if (_board.Cells.Any(cell => cell.CurrentValue != _generatedPuzzle.Solution.GetValue(cell.Position)))
				return new GameSessionCompletion(SessionCompletionStatus.IncorrectSolution, null);

			_timer.Pause();
			_summary = new SessionSummary(_generatedPuzzle.RequestedDifficulty, _timer.Elapsed, _errorCount, _displayedHintLevelCount);
		}
		RaiseViewStateChanged();
		return new GameSessionCompletion(SessionCompletionStatus.Completed, Summary);
	}

	private bool RestoreHistory(bool undo)
	{
		bool valuesChanged;
		lock (_sync)
		{
			if (_summary is not null)
				return false;
			var snapshot = undo ? _history.Undo() : _history.Redo();
			if (snapshot is null)
				return false;
			valuesChanged = !SessionHistory.BoardsHaveSameValues(_board, snapshot.Board);
			_board = snapshot.Board;
			_notes = snapshot.Notes;
			UpdateValidation(_validateBoard.Execute(_board));
			if (valuesChanged)
				_boardRevision++;
		}
		if (!valuesChanged)
		{
			RaiseViewStateChanged();
			return true;
		}

		OnBoardValuesChanged();
		return true;
	}

	private void OnBoardValuesChanged()
	{
		_hintCoordinator.OnBoardValuesChanged();
		RaiseViewStateChanged();
	}

	private HintBoardSnapshot GetHintBoardSnapshot()
	{
		lock (_sync)
			return new HintBoardSnapshot(_board, _boardRevision, _isBoardValid);
	}

	private void OnHintCoordinatorStateChanged(object? sender, GameSessionHintStateChangedEventArgs eventArgs)
	{
		lock (_sync)
		{
			if (eventArgs.BoardRevision != _boardRevision)
				return;
			_hint = eventArgs.Hint;
			_displayedHintLevelCount = eventArgs.DisplayedHintLevelCount;
			_hintAnalysisFailed = eventArgs.AnalysisFailed;
		}
		RaiseViewStateChanged();
	}

	private GameSessionViewState CreateViewState()
	{
		var hintRoles = _hint?.Highlights ?? new Dictionary<HintHighlightRole, IReadOnlyList<CellPosition>>();
		var cells = _board.Cells.Select(cell => new GameSessionCellViewState(
			cell.Position,
			cell.CurrentValue,
			cell.GivenValue.HasValue,
			_selectedPosition == cell.Position,
			_conflictingPositions.Contains(cell.Position),
			_notes.For(cell.Position),
			hintRoles.Where(role => role.Value.Contains(cell.Position)).Select(role => role.Key)));
		var cages = _board.Cages.Select(cage =>
		{
			var values = cage.Positions.Select(position => _board.GetCell(position).CurrentValue).ToArray();
			var isSatisfied = values.All(static value => value.HasValue)
				&& values.Select(static value => value.GetValueOrDefault()).Distinct().Count() == values.Length
				&& values.Sum(static value => value.GetValueOrDefault()) == cage.TargetSum;
			return new GameSessionCageViewState(cage.TargetSum, cage.Positions, isSatisfied);
		});
		return new GameSessionViewState(
			_generatedPuzzle.RequestedDifficulty,
			_selectedPosition,
			_isBoardValid,
			cells,
			cages,
			_inputMode,
			_history.CanUndo,
			_history.CanRedo,
			_timer.IsPaused,
			_timer.Elapsed,
			_boardRevision,
			_errorCount,
			_displayedHintLevelCount,
			_hint,
			_summary);
	}

	private void UpdateValidation(BoardValidationResult validation)
	{
		_conflictingPositions = validation.Conflicts.SelectMany(conflict => conflict.Positions).ToHashSet();
		_isBoardValid = validation.IsValid;
	}

	private void RaiseViewStateChanged() => ViewStateChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed record GameSessionPersistenceState(
	GeneratedPuzzle GeneratedPuzzle,
	SudokuBoard Board,
	CandidateNotes Notes,
	SessionHistoryState History,
	TimeSpan ActiveElapsed,
	int ErrorCount,
	int DisplayedHintLevelCount);
