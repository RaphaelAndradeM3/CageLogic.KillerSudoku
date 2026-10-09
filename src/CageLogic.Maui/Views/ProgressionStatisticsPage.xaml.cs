using CageLogic.Maui.ViewModels;

namespace CageLogic.Maui.Views;

public partial class ProgressionStatisticsPage : ContentPage
{
	private readonly ProgressionStatisticsViewModel _viewModel;

	public ProgressionStatisticsPage(ProgressionStatisticsViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.LoadAsync();
	}
}
