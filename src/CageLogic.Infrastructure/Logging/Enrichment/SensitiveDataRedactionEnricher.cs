using Serilog.Core;
using Serilog.Events;

namespace CageLogic.Infrastructure.Logging.Enrichment;

internal sealed class SensitiveDataRedactionEnricher : ILogEventEnricher
{
	private static readonly string[] SensitivePropertyFragments =
	[
		"password",
		"secret",
		"token",
		"authorization",
		"email",
		"phone",
		"playeranswer",
		"answer",
		"solution",
		"board",
		"payload",
		"puzzle",
		"candidate",
		"notes"
	];

	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		foreach (var property in logEvent.Properties)
		{
			if (SensitivePropertyFragments.Any(fragment => property.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, "[REDACTED]"));
		}
	}
}
