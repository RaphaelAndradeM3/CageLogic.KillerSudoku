namespace CageLogic.Application.GameSessions;

public enum SessionCompletionStatus
{
	Completed,
	Incomplete,
	ConflictingBoard,
	IncorrectSolution
}

public sealed record GameSessionCompletion(SessionCompletionStatus Status, SessionSummary? Summary)
{
	public bool IsCompleted => Status == SessionCompletionStatus.Completed;
}
