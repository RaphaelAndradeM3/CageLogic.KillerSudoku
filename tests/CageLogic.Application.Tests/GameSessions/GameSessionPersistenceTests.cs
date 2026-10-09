using CageLogic.Application.GameSessions;
using CageLogic.Domain.Board;

namespace CageLogic.Application.Tests.GameSessions;

public sealed class GameSessionPersistenceTests
{
	[Test]
	public async Task CaptureAndRestore_RoundTripsPuzzleSolutionBoardNotesHistoryMetricsAndElapsedTime()
	{
		var timeProvider = new ManualTimeProvider();
		var generatedPuzzle = GameSessionTestData.CreateGeneratedPuzzle();
		var session = new GameSession(generatedPuzzle, timeProvider: timeProvider);
		var valuePosition = new CellPosition(0, 0);
		var notePosition = new CellPosition(0, 1);
		var secondValuePosition = new CellPosition(1, 0);

		session.Start();
		timeProvider.Advance(TimeSpan.FromSeconds(37));
		var displayedHint = await session.RequestNextHintAsync().WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(displayedHint?.Status, Is.EqualTo(CageLogic.Application.Hints.HintStatus.Available));
		var firstConflictPosition = new CellPosition(2, 0);
		var secondConflictPosition = new CellPosition(2, 1);
		session.SelectCell(firstConflictPosition);
		Assert.That(session.EnterDigit(4), Is.True);
		session.SelectCell(secondConflictPosition);
		Assert.That(session.EnterDigit(4), Is.True);
		Assert.That(session.EnterDigit(5), Is.True);
		session.SelectCell(valuePosition);
		Assert.That(session.EnterDigit(generatedPuzzle.Solution.GetValue(valuePosition)), Is.True);
		session.SetInputMode(GameInputMode.Candidate);
		session.SelectCell(notePosition);
		Assert.That(session.EnterDigit(9), Is.True);
		session.SetInputMode(GameInputMode.Answer);
		session.SelectCell(secondValuePosition);
		Assert.That(session.EnterDigit(generatedPuzzle.Solution.GetValue(secondValuePosition)), Is.True);
		Assert.That(session.Undo(), Is.True);
		session.Pause();
		Assert.That(session.CanUndo, Is.True);
		Assert.That(session.CanRedo, Is.True);
		Assert.That(session.ErrorCount, Is.GreaterThan(0));
		Assert.That(session.DisplayedHintLevelCount, Is.EqualTo(1));

		var mapper = new GameSessionPersistenceMapper();
		var snapshot = mapper.Capture(session);
		var json = mapper.Serialize(snapshot);
		var restored = mapper.Restore(mapper.Deserialize(json), new ManualTimeProvider());

		Assert.That(snapshot.Version, Is.EqualTo(GameSessionPersistenceSnapshot.CurrentVersion));
		Assert.That(snapshot.Solution, Is.EqualTo(generatedPuzzle.Solution.Values));
		Assert.That(restored.ViewState.Difficulty, Is.EqualTo(session.ViewState.Difficulty));
		Assert.That(restored.CurrentBoardSnapshot.Cells.Select(cell => cell.CurrentValue),
			Is.EqualTo(session.CurrentBoardSnapshot.Cells.Select(cell => cell.CurrentValue)));
		Assert.That(restored.CurrentBoardSnapshot.Cages.Select(cage => cage.TargetSum),
			Is.EqualTo(session.CurrentBoardSnapshot.Cages.Select(cage => cage.TargetSum)));
		Assert.That(restored.ViewState.Cells.Select(cell => cell.Notes.ToArray()),
			Is.EqualTo(session.ViewState.Cells.Select(cell => cell.Notes.ToArray())));
		Assert.That(restored.CanUndo, Is.True);
		Assert.That(restored.CanRedo, Is.True);
		Assert.That(restored.ErrorCount, Is.EqualTo(session.ErrorCount));
		Assert.That(restored.DisplayedHintLevelCount, Is.EqualTo(session.DisplayedHintLevelCount));
		Assert.That(restored.ActiveElapsed, Is.EqualTo(TimeSpan.FromSeconds(37)));
		Assert.That(restored.IsPaused, Is.True);
		Assert.That(restored.ViewState.SelectedPosition, Is.Null);
		Assert.That(restored.InputMode, Is.EqualTo(GameInputMode.Answer));
		restored.Start();
		Assert.That(restored.IsPaused, Is.True, "Navigating to the restored board must not restart elapsed time.");
		restored.Resume();
		Assert.That(restored.IsPaused, Is.False, "Only an explicit player action resumes a restored timer.");
		restored.Pause();

		Assert.That(restored.Redo(), Is.True);
		Assert.That(restored.CurrentBoardSnapshot.GetCell(secondValuePosition).CurrentValue,
			Is.EqualTo(generatedPuzzle.Solution.GetValue(secondValuePosition)));
		Assert.That(restored.Undo(), Is.True);
		Assert.That(restored.CurrentBoardSnapshot.GetCell(secondValuePosition).CurrentValue, Is.Null);
	}

	[Test]
	public void Restore_RejectsUnknownVersionAndMalformedPuzzleOrHistory()
	{
		var mapper = new GameSessionPersistenceMapper();
		var snapshot = mapper.Capture(new GameSession(GameSessionTestData.CreateGeneratedPuzzle()));

		Assert.Throws<InvalidDataException>(() => mapper.Restore(snapshot with { Version = snapshot.Version + 1 }));
		Assert.Throws<InvalidDataException>(() => mapper.Restore(snapshot with { Solution = [.. snapshot.Solution.Skip(1)] }));
		Assert.Throws<InvalidDataException>(() => mapper.Restore(snapshot with { CurrentValues = new int?[80] }));
		Assert.Throws<InvalidDataException>(() => mapper.Restore(snapshot with { ErrorCount = -1 }));
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private long _timestamp;

		public override long TimestampFrequency => TimeSpan.TicksPerSecond;

		public override long GetTimestamp() => _timestamp;

		public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
	}
}
