using Microsoft.Extensions.Logging;

using CageLogic.Infrastructure.Logging;

namespace CageLogic.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.Logging.AddProvider(SerilogLoggingConfiguration.CreateLoggerProvider(new LoggingOptions
		{
			LogDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs"),
			RetentionDays = 14,
			TimeZone = TimeZoneInfo.Local
		}));
		builder.Services.AddSingleton<Logging.HostExceptionBoundary>();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		return builder.Build();
	}
}
