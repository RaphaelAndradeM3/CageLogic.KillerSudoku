using Microsoft.Extensions.Logging;

using CageLogic.Infrastructure.Logging;

using CageLogic.Application.Candidates;
using CageLogic.Application.Generation;
using CageLogic.Application.Hints;
using CageLogic.Application.Moves;
using CageLogic.Application.Validation;
using CageLogic.Maui.ViewModels;
using CageLogic.Maui.Views;

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
		builder.Services.AddSingleton<GeneratePuzzleUseCase>();
		builder.Services.AddSingleton<GetHintUseCase>();
		builder.Services.AddSingleton<ApplyMoveUseCase>();
		builder.Services.AddSingleton<GetCandidatesUseCase>();
		builder.Services.AddSingleton<ValidateBoardUseCase>();
		builder.Services.AddSingleton<GameSessionStore>();
		builder.Services.AddSingleton<HomeViewModel>();
		builder.Services.AddSingleton<HomePage>();
		builder.Services.AddTransient<GamePageViewModel>();
		builder.Services.AddTransient<GamePage>();
		builder.Services.AddTransient<SessionSummaryPage>();
		builder.Services.AddSingleton<AppShell>();
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
