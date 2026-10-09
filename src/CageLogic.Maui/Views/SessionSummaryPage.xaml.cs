using CageLogic.Maui.ViewModels;
using Microsoft.Extensions.Logging;

namespace CageLogic.Maui.Views;

public partial class SessionSummaryPage : ContentPage
{
	private readonly ILogger<SessionSummaryPage> _logger;

	public SessionSummaryPage(GameSessionStore sessionStore, ILogger<SessionSummaryPage> logger)
	{
		InitializeComponent();
		_logger = logger;
		Summary = sessionStore.Current?.Summary ?? throw new InvalidOperationException("A completed session summary is required.");
		BindingContext = this;
	}

	public CageLogic.Application.GameSessions.SessionSummary Summary { get; }

	private async void OnHomeClicked(object? sender, EventArgs eventArgs)
	{
		try
		{
			await Shell.Current.GoToAsync("//home");
		}
		catch (Exception exception)
		{
			var correlationId = Guid.NewGuid().ToString("N");
			_logger.LogError(exception, "Summary navigation failed (CorrelationId {CorrelationId})", correlationId);
			try
			{
				await DisplayAlertAsync("Erro inesperado", $"Não foi possível voltar ao início. Código: {correlationId}", "OK");
			}
			catch (Exception displayFailure)
			{
				_logger.LogError(displayFailure, "Could not display the summary navigation error");
			}
		}
	}
}
