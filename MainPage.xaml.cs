using WeatherApp.ViewModels;

namespace WeatherApp;

public partial class MainPage : ContentPage
{
	private readonly WeatherViewModel _viewModel;

	public MainPage(WeatherViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _viewModel.InitializeAsync();
	}
}
