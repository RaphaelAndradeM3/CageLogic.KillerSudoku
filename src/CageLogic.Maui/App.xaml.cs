using CageLogic.Maui.Logging;

namespace CageLogic.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
	private readonly AppShell _shell;

	public App(HostExceptionBoundary exceptionBoundary, AppShell shell)
	{
		InitializeComponent();
		exceptionBoundary.Register(this);
		_shell = shell;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_shell);
	}
}
