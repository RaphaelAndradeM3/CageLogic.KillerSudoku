using System.Collections.Concurrent;
using CageLogic.Infrastructure.Logging;
using Microsoft.Extensions.Logging;

namespace CageLogic.Infrastructure.Tests.Logging;

public sealed class DailyFileLoggingTests
{
	[Test]
	public void CreateLoggerProvider_WritesMessagesAndErrorsToSeparateDailyFiles()
	{
		WithTemporaryDirectory(directory =>
		{
			using var logging = CreateLoggingSession(directory);
			var logger = logging.LoggerFactory.CreateLogger("session");

			logger.LogInformation("general entry");
			logger.LogError("failure entry");
			logging.Dispose();

			var messageLog = File.ReadAllText(FindLog(directory, "messages-*.log"));
			var errorLog = File.ReadAllText(FindLog(directory, "errors-*.log"));

			Assert.That(messageLog, Does.Contain("general entry"));
			Assert.That(errorLog, Does.Contain("failure entry"));
			Assert.That(errorLog, Does.Not.Contain("general entry"));
		});
	}

	[Test]
	public void CreateLoggerProvider_WritesConfiguredOffsetAndCorrelationContext()
	{
		WithTemporaryDirectory(directory =>
		{
			var timeZone = TimeZoneInfo.CreateCustomTimeZone(
				"UTC+02",
				TimeSpan.FromHours(2),
				"UTC+02",
				"UTC+02");
			using var logging = CreateLoggingSession(directory, timeZone: timeZone);
			var logger = logging.LoggerFactory.CreateLogger("session");

			using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = "corr-0042" }))
				logger.LogInformation("session started");
			logging.Dispose();

			var messageLog = File.ReadAllText(FindLog(directory, "messages-*.log"));
			Assert.That(messageLog, Does.Contain("+02:00"));
			Assert.That(messageLog, Does.Contain("CorrelationId"));
			Assert.That(messageLog, Does.Contain("corr-0042"));
		});
	}

	[Test]
	public void CreateLoggerProvider_DeletesFilesOlderThanConfiguredRetention()
	{
		WithTemporaryDirectory(directory =>
		{
			var expiredMessageLog = Path.Combine(directory, "messages-20000101.log");
			var expiredErrorLog = Path.Combine(directory, "errors-20000101.log");
			var unrelatedFile = Path.Combine(directory, "keep.txt");
			File.WriteAllText(expiredMessageLog, "old");
			File.WriteAllText(expiredErrorLog, "old");
			File.WriteAllText(unrelatedFile, "keep");

			using (CreateLoggingSession(directory, retentionDays: 2))
			{
				Assert.That(File.Exists(expiredMessageLog), Is.False);
				Assert.That(File.Exists(expiredErrorLog), Is.False);
				Assert.That(File.Exists(unrelatedFile), Is.True);
			}
		});
	}

	[Test]
	public void CreateLoggerProvider_RedactsSensitiveStructuredValues()
	{
		WithTemporaryDirectory(directory =>
		{
			using var logging = CreateLoggingSession(directory);
			logging.LoggerFactory.CreateLogger("session").LogInformation(
				"Move rejected for {PlayerAnswer} against {SolutionGrid}",
				"answer-secret-9",
				"solution-secret-731");
			logging.Dispose();

			var messageLog = File.ReadAllText(FindLog(directory, "messages-*.log"));
			Assert.That(messageLog, Does.Contain("[REDACTED]"));
			Assert.That(messageLog, Does.Not.Contain("answer-secret-9"));
			Assert.That(messageLog, Does.Not.Contain("solution-secret-731"));
		});
	}

	[Test]
	public void CreateLoggerProvider_RedactsCredentialPatternsAndOmitsRawExceptionDetails()
	{
		WithTemporaryDirectory(directory =>
		{
			using var logging = CreateLoggingSession(directory);
			var logger = logging.LoggerFactory.CreateLogger("session");
			logger.LogInformation("Request rejected: token=literal-secret {Details}", "authorization: Bearer nested-secret");
			logger.LogError(new InvalidOperationException("password=exception-secret at C:\\Users\\private\\state.json"), "Action failed");
			logging.Dispose();

			var messageLog = File.ReadAllText(FindLog(directory, "messages-*.log"));
			var errorLog = File.ReadAllText(FindLog(directory, "errors-*.log"));
			Assert.That(messageLog, Does.Contain("token=[REDACTED]"));
			Assert.That(messageLog, Does.Contain("authorization: [REDACTED]"));
			Assert.That(messageLog, Does.Not.Contain("literal-secret"));
			Assert.That(messageLog, Does.Not.Contain("nested-secret"));
			Assert.That(errorLog, Does.Contain(nameof(InvalidOperationException)));
			Assert.That(errorLog, Does.Not.Contain("exception-secret"));
			Assert.That(errorLog, Does.Not.Contain("C:\\Users\\private"));
		});
	}

	[Test]
	public void CreateLoggerProvider_UsesFallbackWhenFileLoggerCannotBeInitialized()
	{
		WithTemporaryDirectory(directory =>
		{
			var fileAtLogDirectory = Path.Combine(directory, "not-a-directory");
			File.WriteAllText(fileAtLogDirectory, "blocks directory creation");
			var fallback = new CapturingLoggerProvider();
			var provider = SerilogLoggingConfiguration.CreateLoggerProvider(
				new LoggingOptions { LogDirectory = fileAtLogDirectory },
				() => fallback);

			using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider));
			loggerFactory.CreateLogger("fallback").LogWarning("fallback is active");

			Assert.That(provider, Is.SameAs(fallback));
			Assert.That(fallback.Messages, Does.Contain("fallback is active"));
		});
	}

	private static LoggingSession CreateLoggingSession(
		string directory,
		TimeZoneInfo? timeZone = null,
		int retentionDays = 14)
	{
		var provider = SerilogLoggingConfiguration.CreateLoggerProvider(new LoggingOptions
		{
			LogDirectory = directory,
			RetentionDays = retentionDays,
			TimeZone = timeZone ?? TimeZoneInfo.Utc
		});

		return new LoggingSession(provider, LoggerFactory.Create(builder => builder.AddProvider(provider)));
	}

	private static string FindLog(string directory, string pattern)
	{
		return Directory.GetFiles(directory, pattern).Single();
	}

	private static void WithTemporaryDirectory(Action<string> action)
	{
		var directory = Path.Combine(Path.GetTempPath(), $"CageLogic.Logging.Tests.{Guid.NewGuid():N}");
		Directory.CreateDirectory(directory);
		try
		{
			action(directory);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	private sealed class CapturingLoggerProvider : ILoggerProvider
	{
		public ConcurrentBag<string> Messages { get; } = [];

		public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

		public void Dispose()
		{
		}
	}

	private sealed class LoggingSession(ILoggerProvider provider, ILoggerFactory loggerFactory) : IDisposable
	{
		public ILoggerFactory LoggerFactory { get; } = loggerFactory;

		public void Dispose()
		{
			LoggerFactory.Dispose();
			provider.Dispose();
		}
	}

	private sealed class CapturingLogger(ConcurrentBag<string> messages) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(
			LogLevel logLevel,
			EventId eventId,
			TState state,
			Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			messages.Add(formatter(state, exception));
		}
	}
}
