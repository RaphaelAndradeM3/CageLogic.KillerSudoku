using CageLogic.Application.Difficulty;

namespace CageLogic.Application.GameSessions;

/// <summary>Final immutable statistics for a successfully completed session.</summary>
public sealed record SessionSummary(
	DifficultyLevel Difficulty,
	TimeSpan ActiveTime,
	int ErrorCount,
	int DisplayedHintLevelCount);
