namespace CageLogic.Application.GameSessions;

/// <summary>Runs session mutations off the caller thread and serializes them in dispatch order.</summary>
public sealed class GameSessionCommandQueue
{
	private readonly object _sync = new();
	private Task _lastCommand = Task.CompletedTask;

	public Task<TResult> ExecuteAsync<TResult>(Func<TResult> command, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(command);
		lock (_sync)
		{
			var previousCommand = _lastCommand;
			var releaseQueue = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_lastCommand = releaseQueue.Task;
			return RunAfterAsync(previousCommand, releaseQueue, command, cancellationToken);
		}
	}

	private static async Task<TResult> RunAfterAsync<TResult>(
		Task previousCommand,
		TaskCompletionSource releaseQueue,
		Func<TResult> command,
		CancellationToken cancellationToken)
	{
		try
		{
			await previousCommand.ConfigureAwait(false);
			cancellationToken.ThrowIfCancellationRequested();
			return await Task.Run(command, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			releaseQueue.TrySetResult();
		}
	}
}
