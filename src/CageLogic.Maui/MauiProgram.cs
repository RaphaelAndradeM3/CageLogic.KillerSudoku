using Microsoft.Extensions.Logging;

using Microsoft.Extensions.DependencyInjection;

using CageLogic.Infrastructure.Logging;

using CageLogic.Application.Candidates;
using CageLogic.Application.Generation;
using CageLogic.Application.GameSessions;
using CageLogic.Application.Hints;
using CageLogic.Application.Moves;
using CageLogic.Application.Validation;
using CageLogic.Application.Progression;
using CageLogic.Infrastructure.Progression;
using CageLogic.Maui.ViewModels;
using CageLogic.Maui.Views;

namespace CageLogic.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		Directory.CreateDirectory(FileSystem.AppDataDirectory);
		var progressionConnectionFactory = new SqliteConnectionFactory(
			Path.Combine(FileSystem.AppDataDirectory, "progression.db"));
		new SqliteSchemaMigrator(progressionConnectionFactory).Migrate();
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
		builder.Services.AddSingleton<IGameProgressStore, SqliteGameProgressStore>();
		builder.Services.AddSingleton<CreateGameProgressUseCase>();
		builder.Services.AddSingleton<LoadActiveGameUseCase>();
		builder.Services.AddSingleton<SaveGameProgressUseCase>();
		builder.Services.AddSingleton<CompleteGameProgressUseCase>();
		builder.Services.AddSingleton<AbandonGameProgressUseCase>();
		builder.Services.AddSingleton<SqliteConnectionFactory>(progressionConnectionFactory);
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
