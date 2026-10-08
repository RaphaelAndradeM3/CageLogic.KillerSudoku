using CageLogic.Maui.ViewModels;

namespace CageLogic.Maui.Views;

public partial class SessionSummaryPage : ContentPage
{
	public SessionSummaryPage(GameSessionStore sessionStore)
	{
		InitializeComponent();
		Summary = sessionStore.Current?.Summary ?? throw new InvalidOperationException("A completed session summary is required.");
		BindingContext = this;
	}

	public CageLogic.Application.GameSessions.SessionSummary Summary { get; }

	private async void OnHomeClicked(object? sender, EventArgs eventArgs) => await Shell.Current.GoToAsync("//home");
}
