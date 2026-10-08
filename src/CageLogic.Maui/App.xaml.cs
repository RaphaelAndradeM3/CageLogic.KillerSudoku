using CageLogic.Maui.Logging;

namespace CageLogic.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App(HostExceptionBoundary exceptionBoundary)
	{
		InitializeComponent();
		exceptionBoundary.Register(this);
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
