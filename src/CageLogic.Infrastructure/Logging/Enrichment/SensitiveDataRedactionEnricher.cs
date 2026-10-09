using System.Globalization;
using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;

namespace CageLogic.Infrastructure.Logging.Enrichment;

internal sealed class SensitiveDataRedactionEnricher : ILogEventEnricher
{
	private const string RedactedValue = "[REDACTED]";
	private static readonly Regex CredentialAssignment = new(
		@"(?<name>\b(?:password|passwd|secret|token|authorization|api[_-]?key|access[_-]?key)\b)(?<separator>\s*[:=]\s*)(?<value>(?:Bearer\s+)?(?:""[^""]*""|'[^']*'|[^\s,;]+))",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	private static readonly Regex BearerCredential = new(
		@"\bBearer\s+[A-Za-z0-9._~+/-]+=*",
		RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
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
		foreach (var property in logEvent.Properties.ToArray())
		{
			if (SensitivePropertyFragments.Any(fragment => property.Key.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(property.Key, RedactedValue));
			else
				logEvent.AddOrUpdateProperty(new LogEventProperty(property.Key, Sanitize(property.Value)));
		}

		if (logEvent.Exception is { } exception)
			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ExceptionType", exception.GetType().Name));

		var renderedMessage = logEvent.MessageTemplate.Render(logEvent.Properties, CultureInfo.InvariantCulture);
		logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("SanitizedMessage", SanitizeText(renderedMessage)));
	}

	private static LogEventPropertyValue Sanitize(LogEventPropertyValue value) => value switch
	{
		ScalarValue { Value: string text } => new ScalarValue(SanitizeText(text)),
		SequenceValue sequence => new SequenceValue(sequence.Elements.Select(Sanitize)),
		StructureValue structure => new StructureValue(
			structure.Properties.Select(property => new LogEventProperty(property.Name, SanitizeProperty(property.Name, property.Value))),
			structure.TypeTag),
		DictionaryValue dictionary => new DictionaryValue(dictionary.Elements.Select(pair =>
			new KeyValuePair<ScalarValue, LogEventPropertyValue>(
				pair.Key,
				pair.Key.Value is string key && IsSensitiveProperty(key) ? new ScalarValue(RedactedValue) : Sanitize(pair.Value)))),
		_ => value
	};

	private static LogEventPropertyValue SanitizeProperty(string name, LogEventPropertyValue value) =>
		IsSensitiveProperty(name) ? new ScalarValue(RedactedValue) : Sanitize(value);

	private static bool IsSensitiveProperty(string name) =>
		SensitivePropertyFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase));

	private static string SanitizeText(string text)
	{
		var redactedAssignments = CredentialAssignment.Replace(
			text,
			match => $"{match.Groups["name"].Value}{match.Groups["separator"].Value}{RedactedValue}");
		return BearerCredential.Replace(redactedAssignments, $"Bearer {RedactedValue}");
	}
}
