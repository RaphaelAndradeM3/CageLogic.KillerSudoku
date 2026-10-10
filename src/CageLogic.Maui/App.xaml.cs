using CageLogic.Maui.Logging;
using CageLogic.Application.Progression;

namespace CageLogic.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
	private readonly AppShell _shell;

	public App(HostExceptionBoundary exceptionBoundary, AppShell shell, ThemePreferenceUseCase themePreference)
	{
		InitializeComponent();
		UserAppTheme = themePreference.Get() switch
		{
			AppThemePreference.Light => AppTheme.Light,
			AppThemePreference.Dark => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
		exceptionBoundary.Register(this);
		_shell = shell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_shell);
	}
}
