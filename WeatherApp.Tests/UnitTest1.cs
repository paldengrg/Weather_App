using System.Threading.Tasks;
using Xunit;
using WeatherApp.Models;
using WeatherApp.Services;
using WeatherApp.ViewModels;

namespace WeatherApp.Tests
{
    public class MockWeatherService : IWeatherService
    {
        public bool GetWeatherCalled { get; private set; }
        public string? LastCity { get; private set; }
        public string? LastUnits { get; private set; }
        public string? LastApiKey { get; private set; }
        public WeatherResponse MockResult { get; set; } = new();

        public Task<WeatherResponse> GetWeatherAsync(string city, string units, string apiKey)
        {
            GetWeatherCalled = true;
            LastCity = city;
            LastUnits = units;
            LastApiKey = apiKey;
            return Task.FromResult(MockResult);
        }
    }

    public class WeatherViewModelTests
    {
        [Fact]
        public async Task Search_WithEmptyCityName_SetsValidationError()
        {
            // Arrange
            var mockService = new MockWeatherService();
            var viewModel = new WeatherViewModel(mockService);
            viewModel.CityQuery = "";

            // Act
            await viewModel.SearchAsync();

            // Assert
            Assert.True(viewModel.HasError);
            Assert.Equal("Please enter a city name.", viewModel.ErrorMessage);
            Assert.False(viewModel.IsWeatherVisible);
            Assert.False(mockService.GetWeatherCalled);
        }

        [Fact]
        public async Task Search_WithValidCityName_FetchesWeatherDataAndSetsVisible()
        {
            // Arrange
            var mockService = new MockWeatherService();
            mockService.MockResult = new WeatherResponse
            {
                Cod = 200,
                Name = "Sydney",
                Sys = new Sys { Country = "AU" },
                Main = new MainData { Temp = 22.0, FeelsLike = 24.0, Humidity = 65, Pressure = 1013 },
                Weather = new[] { new WeatherItem { Description = "scattered clouds", Icon = "03d" } }
            };

            var viewModel = new WeatherViewModel(mockService);
            viewModel.CityQuery = "Sydney";

            // Act
            await viewModel.SearchAsync();

            // Assert
            Assert.False(viewModel.HasError);
            Assert.True(viewModel.IsWeatherVisible);
            Assert.Equal("Sydney, AU", viewModel.DisplayCity);
            Assert.Equal("22°C", viewModel.DisplayTemp);
            Assert.Equal("24°C", viewModel.DisplayFeelsLike);
            Assert.Equal("65%", viewModel.DisplayHumidity);
            Assert.Equal("1013 hPa", viewModel.DisplayPressure);
            Assert.Equal("Scattered Clouds", viewModel.DisplayDescription);
            Assert.Equal("https://openweathermap.org/img/wn/03d@2x.png", viewModel.DisplayIconUrl);
            Assert.True(mockService.GetWeatherCalled);
            Assert.Equal("Sydney", mockService.LastCity);
        }

        [Fact]
        public async Task Search_WhenApiFails_SetsErrorMessage()
        {
            // Arrange
            var mockService = new MockWeatherService();
            mockService.MockResult = new WeatherResponse
            {
                Cod = 404,
                Message = "city not found"
            };

            var viewModel = new WeatherViewModel(mockService);
            viewModel.CityQuery = "FakeCity";

            // Act
            await viewModel.SearchAsync();

            // Assert
            Assert.True(viewModel.HasError);
            Assert.Equal("city not found", viewModel.ErrorMessage);
            Assert.False(viewModel.IsWeatherVisible);
            Assert.True(mockService.GetWeatherCalled);
        }

        [Fact]
        public void ToggleUnits_SwitchesMetricImperialAndRefreshes()
        {
            // Arrange
            var mockService = new MockWeatherService();
            mockService.MockResult = new WeatherResponse
            {
                Cod = 200,
                Name = "London",
                Sys = new Sys { Country = "GB" },
                Main = new MainData { Temp = 15.0, FeelsLike = 14.0, Humidity = 80, Pressure = 1009 },
                Weather = new[] { new WeatherItem { Description = "light rain", Icon = "10d" } }
            };

            var viewModel = new WeatherViewModel(mockService);
            viewModel.CityQuery = "London";
            viewModel.IsWeatherVisible = true;
            viewModel.WeatherInfo = mockService.MockResult;

            Assert.True(viewModel.IsMetric);
            Assert.Equal("°C", viewModel.TempSymbol);

            // Act
            viewModel.ToggleUnits();

            // Assert
            Assert.False(viewModel.IsMetric);
            Assert.Equal("°F", viewModel.TempSymbol);
            Assert.Equal("mph", viewModel.SpeedSymbol);
            // Verify that search is re-triggered automatically
            Assert.True(mockService.GetWeatherCalled);
            Assert.Equal("imperial", mockService.LastUnits);
        }

        [Fact]
        public void SavedApiKey_UpdatesHasCustomApiKey()
        {
            // Arrange
            var mockService = new MockWeatherService();
            var viewModel = new WeatherViewModel(mockService);

            // Assert default state
            Assert.False(viewModel.HasCustomApiKey);

            // Act
            viewModel.SavedApiKey = "some_api_key";

            // Assert updated state
            Assert.True(viewModel.HasCustomApiKey);
        }

        [Fact]
        public void ToggleSettings_TogglesIsSettingsVisible()
        {
            // Arrange
            var mockService = new MockWeatherService();
            var viewModel = new WeatherViewModel(mockService);

            // Assert default state
            Assert.False(viewModel.IsSettingsVisible);

            // Act & Assert toggle to true
            viewModel.ToggleSettings();
            Assert.True(viewModel.IsSettingsVisible);

            // Act & Assert toggle to false
            viewModel.ToggleSettings();
            Assert.False(viewModel.IsSettingsVisible);
        }
    }
}
