using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace CageLogic.Maui.Logging;

public sealed class HostExceptionBoundary(ILogger<HostExceptionBoundary> logger)
{
	private readonly object _handledExceptionsLock = new();
	private readonly ConditionalWeakTable<Exception, object> _handledExceptions = new();
	private bool _registered;

	public void Register(Microsoft.Maui.Controls.Application application)
	{
		ArgumentNullException.ThrowIfNull(application);
		if (_registered)
			return;

		_registered = true;
		AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
		{
			Handle(eventArgs.ExceptionObject as Exception, "AppDomain.UnhandledException");
		};
		TaskScheduler.UnobservedTaskException += (_, eventArgs) =>
		{
			Handle(eventArgs.Exception, "TaskScheduler.UnobservedTaskException");
			eventArgs.SetObserved();
		};
	}

	private void Handle(Exception? exception, string source)
	{
		if (exception is not null && !TryMarkHandled(exception))
			return;

		var correlationId = Guid.NewGuid().ToString("N");
		try
		{
			using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
			{
				if (exception is null)
					logger.LogCritical("Unhandled host exception from {ExceptionSource} (CorrelationId {CorrelationId})", source, correlationId);
				else
					logger.LogCritical(exception, "Unhandled host exception from {ExceptionSource} (CorrelationId {CorrelationId})", source, correlationId);
			}
		}
		catch (Exception loggingFailure)
		{
			Trace.WriteLine($"Unhandled host exception; correlation {correlationId}; logging fallback {loggingFailure.GetType().Name}");
		}

		try
		{
			MainThread.BeginInvokeOnMainThread(async () =>
			{
				try
				{
					var page = Microsoft.Maui.Controls.Application.Current?.Windows
						.FirstOrDefault(window => window.Page is not null)?.Page;
					if (page is not null)
						await page.DisplayAlertAsync("Erro inesperado", $"Ocorreu um erro inesperado. Código: {correlationId}", "OK");
				}
				catch (Exception displayFailure)
				{
					Trace.WriteLine($"Could not display safe host error message: {displayFailure.GetType().Name}");
				}
			});
		}
		catch (Exception dispatchFailure)
		{
			Trace.WriteLine($"Could not dispatch safe host error message: {dispatchFailure.GetType().Name}");
		}
	}

	private bool TryMarkHandled(Exception exception)
	{
		lock (_handledExceptionsLock)
		{
			if (_handledExceptions.TryGetValue(exception, out _))
				return false;

			_handledExceptions.Add(exception, new object());
			return true;
		}
	}
}
