using CageLogic.Application.Difficulty;

namespace CageLogic.Application.Progression;

public enum GameProgressStatus
{
	Active,
	Completed,
	Abandoned
}

public sealed record GameProgressRecord(
	Guid SessionId,
	GameProgressStatus Status,
	DateTimeOffset StartedAtUtc,
	DateTimeOffset? CompletedAtUtc,
	DateTimeOffset? AbandonedAtUtc,
	DifficultyLevel Difficulty,
	long ActiveElapsedTicks,
	int ErrorCount,
	int DisplayedHintLevelCount);

/// <summary>Durable snapshot for the single active game session.</summary>
public sealed record SavedGameSession(
	GameProgressRecord Record,
	int SnapshotVersion,
	string SnapshotJson)
{
	public Guid SessionId => Record.SessionId;
}

public enum GameProgressLoadStatus
{
	Loaded,
	NoActiveSession,
	RecoveryRequired
}

public sealed record GameProgressLoadResult(
	GameProgressLoadStatus Status,
	SavedGameSession? Session = null,
	string? RecoveryReason = null)
{
	public static GameProgressLoadResult Loaded(SavedGameSession session) =>
		new(GameProgressLoadStatus.Loaded, session);

	public static GameProgressLoadResult NoActiveSession() =>
		new(GameProgressLoadStatus.NoActiveSession);

	public static GameProgressLoadResult RecoveryRequired(string reason) =>
		new(GameProgressLoadStatus.RecoveryRequired, RecoveryReason: reason);
}

public enum GameProgressWriteStatus
{
	Saved,
	Conflict,
	Failed
}

public sealed record GameProgressWriteResult(GameProgressWriteStatus Status, string? FailureReason = null)
{
	public bool IsSaved => Status == GameProgressWriteStatus.Saved;

	public static GameProgressWriteResult Saved() => new(GameProgressWriteStatus.Saved);

	public static GameProgressWriteResult Conflict(string reason) => new(GameProgressWriteStatus.Conflict, reason);

	public static GameProgressWriteResult Failed(string reason) => new(GameProgressWriteStatus.Failed, reason);
}
