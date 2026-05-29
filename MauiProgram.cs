using Microsoft.Extensions.Logging;
using System.Net.Http;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Register Services and ViewModels
		builder.Services.AddSingleton<HttpClient>();
		builder.Services.AddSingleton<IWeatherService, WeatherService>();
		builder.Services.AddSingleton<WeatherViewModel>();
		builder.Services.AddSingleton<MainPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
