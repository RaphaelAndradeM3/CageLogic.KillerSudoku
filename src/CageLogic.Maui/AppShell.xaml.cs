namespace CageLogic.Maui;

public partial class AppShell : Shell
{
	public AppShell(CageLogic.Maui.Views.HomePage homePage)
	{
		InitializeComponent();
		Items.Clear();
		Items.Add(new ShellContent
		{
			Title = "Killer Sudoku",
			Route = "home",
			Content = homePage
		});
		Routing.RegisterRoute(nameof(CageLogic.Maui.Views.GamePage), typeof(CageLogic.Maui.Views.GamePage));
		Routing.RegisterRoute(nameof(CageLogic.Maui.Views.SessionSummaryPage), typeof(CageLogic.Maui.Views.SessionSummaryPage));
		Routing.RegisterRoute(nameof(CageLogic.Maui.Views.ProgressionStatisticsPage), typeof(CageLogic.Maui.Views.ProgressionStatisticsPage));
		Routing.RegisterRoute(nameof(CageLogic.Maui.Views.ThemeSettingsPage), typeof(CageLogic.Maui.Views.ThemeSettingsPage));
	}
}
