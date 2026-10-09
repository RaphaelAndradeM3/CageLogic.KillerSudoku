using System.Diagnostics;
using System.Globalization;
using CageLogic.Infrastructure.Logging.Enrichment;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace CageLogic.Infrastructure.Logging;

public static class SerilogLoggingConfiguration
{
	private const string OutputTemplate = "{LocalTimestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SanitizedMessage} {Properties:j}{NewLine}";

	public static ILoggerProvider CreateLoggerProvider(
		LoggingOptions options,
		Func<ILoggerProvider>? fallbackProviderFactory = null)
	{
		ArgumentNullException.ThrowIfNull(options);

		try
		{
			Validate(options);
			Directory.CreateDirectory(options.LogDirectory);
			DeleteExpiredFiles(options);

			var logger = new LoggerConfiguration()
				.MinimumLevel.Verbose()
				.Enrich.FromLogContext()
				.Enrich.With(new LocalTimestampEnricher(options.TimeZone))
				.Enrich.With(new SensitiveDataRedactionEnricher())
				.WriteTo.File(
					Path.Combine(options.LogDirectory, "messages-.log"),
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: options.RetentionDays,
					outputTemplate: OutputTemplate,
					restrictedToMinimumLevel: LogEventLevel.Information,
					buffered: false)
				.WriteTo.File(
					Path.Combine(options.LogDirectory, "errors-.log"),
					rollingInterval: RollingInterval.Day,
					retainedFileCountLimit: options.RetentionDays,
					outputTemplate: OutputTemplate,
					restrictedToMinimumLevel: LogEventLevel.Error,
					buffered: false)
				.CreateLogger();

			return new SerilogLoggerProvider(logger, dispose: true);
		}
		catch (Exception exception)
		{
			Trace.WriteLine($"Could not initialize file logging: {exception.GetType().Name}");
			return CreateFallbackProvider(fallbackProviderFactory);
		}
	}

	private static void Validate(LoggingOptions options)
	{
		if (string.IsNullOrWhiteSpace(options.LogDirectory))
			throw new ArgumentException("A log directory is required.", nameof(options));

		if (options.RetentionDays < 1)
			throw new ArgumentOutOfRangeException(nameof(options), "Retention must be at least one day.");

		ArgumentNullException.ThrowIfNull(options.TimeZone);
	}

	private static void DeleteExpiredFiles(LoggingOptions options)
	{
		var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, options.TimeZone).DateTime);
		var oldestRetainedDate = today.AddDays(1 - options.RetentionDays);

		foreach (var filePath in Directory.EnumerateFiles(options.LogDirectory))
		{
			var fileName = Path.GetFileNameWithoutExtension(filePath);
			if (!TryGetLogDate(fileName, out var logDate) || logDate >= oldestRetainedDate)
				continue;

			try
			{
				File.Delete(filePath);
			}
			catch (IOException exception)
			{
				Trace.WriteLine($"Could not remove expired log file: {exception.GetType().Name}");
			}
			catch (UnauthorizedAccessException exception)
			{
				Trace.WriteLine($"Could not remove expired log file: {exception.GetType().Name}");
			}
		}
	}

	private static bool TryGetLogDate(string fileName, out DateOnly logDate)
	{
		logDate = default;
		var separator = fileName.IndexOf('-');
		if (separator < 0 || fileName.Length != separator + 9)
			return false;

		var prefix = fileName[..separator];
		if (!string.Equals(prefix, "messages", StringComparison.Ordinal) &&
			!string.Equals(prefix, "errors", StringComparison.Ordinal))
			return false;

		return DateOnly.TryParseExact(
			fileName.AsSpan(separator + 1),
			"yyyyMMdd",
			CultureInfo.InvariantCulture,
			DateTimeStyles.None,
			out logDate);
	}

	private static ILoggerProvider CreateFallbackProvider(Func<ILoggerProvider>? fallbackProviderFactory)
	{
		if (fallbackProviderFactory is not null)
		{
			try
			{
				return fallbackProviderFactory();
			}
			catch (Exception exception)
			{
				Trace.WriteLine($"Could not initialize configured logging fallback: {exception.GetType().Name}");
			}
		}

		return new TraceLoggerProvider();
	}
}

internal sealed class TraceLoggerProvider : ILoggerProvider
{
	public ILogger CreateLogger(string categoryName) => new TraceLogger(categoryName);

	public void Dispose()
	{
	}

	private sealed class TraceLogger(string categoryName) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			try
			{
				var exceptionText = exception is null ? string.Empty : $" {exception.GetType().Name}";
				Trace.WriteLine($"{DateTimeOffset.Now:O} [{logLevel}] {categoryName}: {formatter(state, exception)}{exceptionText}");
			}
			catch
			{
				// The fallback deliberately has no dependencies that could fail with the file logger.
			}
		}
	}
}
