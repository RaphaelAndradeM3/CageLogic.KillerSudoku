namespace CageLogic.Infrastructure.Logging;

public sealed class LoggingOptions
{
	public string LogDirectory { get; init; } = string.Empty;

	public int RetentionDays { get; init; } = 14;

	public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Local;
}
