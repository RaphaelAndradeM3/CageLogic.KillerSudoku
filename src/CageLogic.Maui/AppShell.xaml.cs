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
	}
}
