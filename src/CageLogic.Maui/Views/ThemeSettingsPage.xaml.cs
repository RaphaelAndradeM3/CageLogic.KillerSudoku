using CageLogic.Maui.ViewModels;

namespace CageLogic.Maui.Views;

public partial class ThemeSettingsPage : ContentPage
{
	private readonly ThemeSettingsViewModel _viewModel;

	public ThemeSettingsPage(ThemeSettingsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_viewModel.Initialize();
	}
}
