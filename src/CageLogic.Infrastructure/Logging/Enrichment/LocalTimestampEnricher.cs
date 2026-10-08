using Serilog.Core;
using Serilog.Events;

namespace CageLogic.Infrastructure.Logging.Enrichment;

internal sealed class LocalTimestampEnricher(TimeZoneInfo timeZone) : ILogEventEnricher
{
	public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
	{
		var timestamp = TimeZoneInfo.ConvertTime(logEvent.Timestamp, timeZone);
		logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("LocalTimestamp", timestamp));
	}
}
