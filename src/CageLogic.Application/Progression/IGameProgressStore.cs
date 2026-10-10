namespace CageLogic.Application.Progression;

/// <summary>Persistence boundary for game-session progress and its statistics records.</summary>
public interface IGameProgressStore
{
	Task<GameProgressWriteResult> CreateAsync(
		SavedGameSession session,
		CancellationToken cancellationToken = default);

	Task<GameProgressLoadResult> LoadActiveAsync(CancellationToken cancellationToken = default);

	Task<GameProgressWriteResult> SaveAsync(
		SavedGameSession session,
		CancellationToken cancellationToken = default);

	Task<GameProgressWriteResult> CompleteAsync(
		GameProgressRecord completedRecord,
		CancellationToken cancellationToken = default);

	Task<GameProgressWriteResult> AbandonAsync(
		GameProgressRecord abandonedRecord,
		CancellationToken cancellationToken = default);

	/// <summary>Moves an unreadable active row to history after the player explicitly confirms replacement.</summary>
	Task<GameProgressWriteResult> AbandonUnrecoverableActiveAsync(
		string sessionKey,
		DateTimeOffset abandonedAtUtc,
		CancellationToken cancellationToken = default);

	/// <summary>Replaces an active row atomically so a failed new-game save cannot discard the old game.</summary>
	Task<GameProgressWriteResult> ReplaceActiveAsync(
		SavedGameSession replacement,
		string? replacedSessionKey,
		DateTimeOffset abandonedAtUtc,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<GameProgressRecord>> GetRecordsAsync(CancellationToken cancellationToken = default);
}
