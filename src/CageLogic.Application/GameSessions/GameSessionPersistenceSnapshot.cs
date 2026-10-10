using System.Text.Json;
using CageLogic.Application.Difficulty;
using CageLogic.Application.Generation;
using CageLogic.Application.Hints;
using CageLogic.Application.Solving;
using CageLogic.Domain.Board;
using CageLogic.Domain.Cages;
using CageLogic.Domain.LogicalSteps;
using CageLogic.Domain.Puzzles;

namespace CageLogic.Application.GameSessions;

/// <summary>Versioned DTO graph used to store and restore a game session.</summary>
public sealed record GameSessionPersistenceSnapshot(
	int Version,
	DifficultyLevel Difficulty,
	int?[] Givens,
	PersistedCage[] Cages,
	int[] Solution,
	int?[] CurrentValues,
	PersistedCellNotes[] Notes,
	PersistedHistory History,
	long ActiveElapsedTicks,
	int ErrorCount,
	int DisplayedHintLevelCount)
{
	public const int CurrentVersion = 1;
}

public sealed record PersistedPosition(int Row, int Column);

public sealed record PersistedCage(int TargetSum, PersistedPosition[] Positions);

public sealed record PersistedCellNotes(PersistedPosition Position, int[] Digits);

public sealed record PersistedHistory(
	PersistedHistoryChange[] Undo,
	PersistedHistoryChange[] Redo);

public sealed record PersistedHistoryChange(
	PersistedBoardState Before,
	PersistedBoardState After);

public sealed record PersistedBoardState(
	int?[] Values,
	PersistedCellNotes[] Notes);

/// <summary>Maps session state to validated, versioned persistence snapshots.</summary>
public sealed class GameSessionPersistenceMapper
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	public GameSessionPersistenceSnapshot Capture(GameSession session)
	{
		ArgumentNullException.ThrowIfNull(session);
		var state = session.CapturePersistenceState();
		var puzzle = state.GeneratedPuzzle.Puzzle;
		var givens = new int?[81];
		foreach (var (position, value) in puzzle.Givens)
			givens[CellIndex(position)] = value;

		var cages = puzzle.Cages
			.Select(cage => new PersistedCage(cage.TargetSum, cage.Positions.Select(ToPersistedPosition).ToArray()))
			.ToArray();
		var history = new PersistedHistory(
			state.History.Undo.Select(CaptureChange).ToArray(),
			state.History.Redo.Select(CaptureChange).ToArray());

		return new GameSessionPersistenceSnapshot(
			GameSessionPersistenceSnapshot.CurrentVersion,
			state.GeneratedPuzzle.RequestedDifficulty,
			givens,
			cages,
			state.GeneratedPuzzle.Solution.Values.ToArray(),
			state.Board.Cells.Select(cell => cell.CurrentValue).ToArray(),
			CaptureNotes(state.Notes),
			history,
			state.ActiveElapsed.Ticks,
			state.ErrorCount,
			state.DisplayedHintLevelCount);
	}

	public string Serialize(GameSessionPersistenceSnapshot snapshot)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		return JsonSerializer.Serialize(snapshot, JsonOptions);
	}

	public GameSessionPersistenceSnapshot Deserialize(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			throw new InvalidDataException("The saved session snapshot is empty.");
		try
		{
			return JsonSerializer.Deserialize<GameSessionPersistenceSnapshot>(json, JsonOptions)
				?? throw new InvalidDataException("The saved session snapshot is empty.");
		}
		catch (JsonException exception)
		{
			throw new InvalidDataException("The saved session snapshot is not valid JSON.", exception);
		}
	}

	public GameSession Restore(
		GameSessionPersistenceSnapshot snapshot,
		TimeProvider? timeProvider = null,
		GetHintUseCase? getHintUseCase = null)
	{
		ArgumentNullException.ThrowIfNull(snapshot);
		try
		{
			ValidateHeader(snapshot);
			var puzzle = RestorePuzzle(snapshot);
			var solution = new SolutionGrid(snapshot.Solution, puzzle);
			var analysis = new DifficultyAnalysisResult(
				DifficultyAnalysisStatus.Classified,
				snapshot.Difficulty,
				catalogVersion: 1,
				Array.Empty<LogicalTechniqueId>());
			var generatedPuzzle = new GeneratedPuzzle(
				puzzle,
				solution,
				analysis,
				snapshot.Difficulty,
				seed: null,
				attempts: 1,
				elapsed: TimeSpan.Zero);

			var board = RestoreBoard(snapshot.CurrentValues, snapshot.Givens, puzzle);
			var notes = RestoreNotes(snapshot.Notes, board);
			var history = RestoreHistory(snapshot.History, puzzle, snapshot.Givens, board, notes);
			return GameSession.Restore(
				generatedPuzzle,
				board,
				notes,
				history,
				TimeSpan.FromTicks(snapshot.ActiveElapsedTicks),
				snapshot.ErrorCount,
				snapshot.DisplayedHintLevelCount,
				timeProvider,
				getHintUseCase);
		}
		catch (InvalidDataException)
		{
			throw;
		}
		catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException)
		{
			throw new InvalidDataException("The saved session snapshot violates game-session invariants.", exception);
		}
	}

	private static void ValidateHeader(GameSessionPersistenceSnapshot snapshot)
	{
		if (snapshot.Version != GameSessionPersistenceSnapshot.CurrentVersion)
			throw new InvalidDataException($"Snapshot version {snapshot.Version} is not supported.");
		if (!Enum.IsDefined(snapshot.Difficulty))
			throw new InvalidDataException("The saved session difficulty is unknown.");
		if (snapshot.Givens is null || snapshot.Givens.Length != 81 ||
			snapshot.Solution is null || snapshot.Solution.Length != 81 ||
			snapshot.CurrentValues is null || snapshot.CurrentValues.Length != 81)
			throw new InvalidDataException("The saved session grids must contain exactly 81 cells.");
		if (snapshot.Cages is null || snapshot.Notes is null || snapshot.History is null ||
			snapshot.History.Undo is null || snapshot.History.Redo is null)
			throw new InvalidDataException("The saved session is missing required puzzle or history data.");
		if (snapshot.ActiveElapsedTicks < 0 || snapshot.ErrorCount < 0 || snapshot.DisplayedHintLevelCount < 0)
			throw new InvalidDataException("The saved session contains a negative duration or counter.");
	}

	private static ValidatedPuzzle RestorePuzzle(GameSessionPersistenceSnapshot snapshot)
	{
		var givens = new Dictionary<PuzzleDefinitionPosition, int>();
		for (var index = 0; index < snapshot.Givens.Length; index++)
		{
			if (snapshot.Givens[index] is { } value)
				givens.Add(ToPuzzlePosition(index), value);
		}

		var cages = snapshot.Cages.Select(cage =>
		{
			if (cage is null || cage.Positions is null)
				throw new InvalidDataException("The saved puzzle contains an incomplete cage.");
			return new CageDefinition(cage.TargetSum, cage.Positions.Select(position =>
				position is null
					? throw new InvalidDataException("The saved puzzle contains an incomplete cage position.")
					: ToPuzzlePosition(position)));
		}).ToArray();
		var validation = new PuzzleStructureValidator().Validate(new PuzzleDefinition(givens, cages));
		if (!validation.IsValid)
			throw new InvalidDataException("The saved puzzle definition is structurally invalid.");
		return validation.Puzzle!;
	}

	private static SudokuBoard RestoreBoard(
		IReadOnlyList<int?> values,
		IReadOnlyList<int?> givens,
		ValidatedPuzzle puzzle)
	{
		var board = puzzle.CreateBoard();
		for (var index = 0; index < 81; index++)
		{
			var position = ToCellPosition(index);
			var expectedGiven = givens[index];
			if (expectedGiven is < 1 or > 9)
				throw new InvalidDataException("The saved puzzle contains an invalid given value.");
			if (board.GetCell(position).GivenValue != expectedGiven)
				throw new InvalidDataException("The saved board givens do not match its puzzle.");

			var value = values[index];
			if (value is < 1 or > 9)
				throw new InvalidDataException("The saved board contains an invalid digit.");
			if (!value.HasValue)
				continue;
			if (expectedGiven.HasValue)
			{
				if (value != expectedGiven)
					throw new InvalidDataException("A saved player value replaces a fixed given.");
				continue;
			}
			board = board.WithPlayerValue(position, value);
		}
		return board;
	}

	private static CandidateNotes RestoreNotes(IReadOnlyList<PersistedCellNotes> entries, SudokuBoard board)
	{
		var notes = CandidateNotes.Empty;
		var positions = new HashSet<CellPosition>();
		foreach (var entry in entries)
		{
			if (entry is null || entry.Position is null || entry.Digits is null)
				throw new InvalidDataException("The saved candidate notes contain an incomplete entry.");
			var position = ToCellPosition(entry.Position);
			if (!positions.Add(position))
				throw new InvalidDataException("The saved candidate notes contain a duplicate cell.");
			if (board.GetCell(position).CurrentValue.HasValue)
				throw new InvalidDataException("The saved candidate notes belong to a filled cell.");
			if (entry.Digits.Length == 0 || entry.Digits.Distinct().Count() != entry.Digits.Length ||
				entry.Digits.Any(digit => digit is < 1 or > 9))
				throw new InvalidDataException("The saved candidate notes contain invalid digits.");
			notes.Set(position, entry.Digits, out notes);
		}
		return notes;
	}

	private static SessionHistoryState RestoreHistory(
		PersistedHistory persisted,
		ValidatedPuzzle puzzle,
		IReadOnlyList<int?> givens,
		SudokuBoard currentBoard,
		CandidateNotes currentNotes)
	{
		var undo = persisted.Undo.Select(change => RestoreChange(change, puzzle, givens)).ToArray();
		var redo = persisted.Redo.Select(change => RestoreChange(change, puzzle, givens)).ToArray();
		var expected = new SessionSnapshot(currentBoard, currentNotes);
		foreach (var change in undo)
		{
			if (!SnapshotsEqual(change.After, expected))
				throw new InvalidDataException("The undo history does not lead to the saved board.");
			expected = change.Before;
		}
		expected = new SessionSnapshot(currentBoard, currentNotes);
		foreach (var change in redo)
		{
			if (!SnapshotsEqual(change.Before, expected))
				throw new InvalidDataException("The redo history does not start at the saved board.");
			expected = change.After;
		}
		return new SessionHistoryState(undo, redo);
	}

	private static SessionHistoryEntry RestoreChange(
		PersistedHistoryChange change,
		ValidatedPuzzle puzzle,
		IReadOnlyList<int?> givens)
	{
		if (change is null || change.Before is null || change.After is null)
			throw new InvalidDataException("The saved history contains an incomplete change.");
		var before = RestoreSnapshot(change.Before, puzzle, givens);
		var after = RestoreSnapshot(change.After, puzzle, givens);
		if (SessionHistory.BoardsHaveSameValues(before.Board, after.Board) && before.Notes.ContentEquals(after.Notes))
			throw new InvalidDataException("The saved history contains an empty change.");
		return new SessionHistoryEntry(before, after);
	}

	private static SessionSnapshot RestoreSnapshot(
		PersistedBoardState persisted,
		ValidatedPuzzle puzzle,
		IReadOnlyList<int?> givens)
	{
		if (persisted.Values is null || persisted.Values.Length != 81 || persisted.Notes is null)
			throw new InvalidDataException("A saved history board must contain 81 values and a notes collection.");
		var board = RestoreBoard(persisted.Values, givens, puzzle);
		return new SessionSnapshot(board, RestoreNotes(persisted.Notes, board));
	}

	private static bool SnapshotsEqual(SessionSnapshot left, SessionSnapshot right) =>
		SessionHistory.BoardsHaveSameValues(left.Board, right.Board) && left.Notes.ContentEquals(right.Notes);

	private static PersistedHistoryChange CaptureChange(SessionHistoryEntry change) =>
		new(CaptureBoardState(change.Before), CaptureBoardState(change.After));

	private static PersistedBoardState CaptureBoardState(SessionSnapshot snapshot) =>
		new(snapshot.Board.Cells.Select(cell => cell.CurrentValue).ToArray(), CaptureNotes(snapshot.Notes));

	private static PersistedCellNotes[] CaptureNotes(CandidateNotes notes) => notes.Notes
		.OrderBy(pair => CellIndex(pair.Key))
		.Select(pair => new PersistedCellNotes(ToPersistedPosition(pair.Key), pair.Value.ToArray()))
		.ToArray();

	private static PersistedPosition ToPersistedPosition(CellPosition position) => new(position.Row, position.Column);

	private static PuzzleDefinitionPosition ToPuzzlePosition(PersistedPosition position) =>
		new(position.Row, position.Column);

	private static PuzzleDefinitionPosition ToPuzzlePosition(int index) =>
		new(index / 9, index % 9);

	private static CellPosition ToCellPosition(PersistedPosition position) =>
		new(position.Row, position.Column);

	private static CellPosition ToCellPosition(int index) =>
		new(index / 9, index % 9);

	private static int CellIndex(CellPosition position) => position.Row * 9 + position.Column;
}
