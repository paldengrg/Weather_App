using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WeatherApp.Models;
using WeatherApp.Services;
using Microsoft.Maui.Storage;

namespace WeatherApp.ViewModels
{
    public partial class WeatherViewModel : ObservableObject
    {
        private readonly IWeatherService _weatherService;

        public WeatherViewModel(IWeatherService weatherService)
        {
            _weatherService = weatherService;
            
            // Default settings
            SelectedUnit = "metric"; 
            IsMetric = true;
            CityQuery = string.Empty;

            // Load saved API key on startup
            Task.Run(async () => await InitializeAsync());
        }

        public async Task InitializeAsync()
        {
            await LoadApiKeyAsync();
        }

        [ObservableProperty]
        public partial string? CityQuery { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWelcomeVisible))]
        public partial bool IsLoading { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWelcomeVisible))]
        public partial bool HasError { get; set; }

        [ObservableProperty]
        public partial string? ErrorMessage { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayCity))]
        [NotifyPropertyChangedFor(nameof(DisplayTemp))]
        [NotifyPropertyChangedFor(nameof(DisplayFeelsLike))]
        [NotifyPropertyChangedFor(nameof(DisplayHumidity))]
        [NotifyPropertyChangedFor(nameof(DisplayWind))]
        [NotifyPropertyChangedFor(nameof(DisplayPressure))]
        [NotifyPropertyChangedFor(nameof(DisplayDescription))]
        [NotifyPropertyChangedFor(nameof(DisplayIconUrl))]
        public partial WeatherResponse? WeatherInfo { get; set; }

        public string DisplayCity => WeatherInfo != null ? $"{WeatherInfo.Name}, {WeatherInfo.Sys?.Country}" : string.Empty;
        public string DisplayTemp => WeatherInfo?.Main != null ? $"{Math.Round(WeatherInfo.Main.Temp)}{TempSymbol}" : string.Empty;
        public string DisplayFeelsLike => WeatherInfo?.Main != null ? $"{Math.Round(WeatherInfo.Main.FeelsLike)}{TempSymbol}" : string.Empty;
        public string DisplayHumidity => WeatherInfo?.Main != null ? $"{WeatherInfo.Main.Humidity}%" : string.Empty;
        public string DisplayWind => WeatherInfo?.Wind != null ? $"{WeatherInfo.Wind.Speed} {SpeedSymbol}" : string.Empty;
        public string DisplayPressure => WeatherInfo?.Main != null ? $"{WeatherInfo.Main.Pressure} hPa" : string.Empty;
        public string DisplayDescription => WeatherInfo != null && WeatherInfo.Weather != null && WeatherInfo.Weather.Length > 0 && WeatherInfo.Weather[0]?.Description != null
            ? System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(WeatherInfo.Weather[0].Description!) 
            : string.Empty;
        public string DisplayIconUrl => WeatherInfo != null && WeatherInfo.Weather != null && WeatherInfo.Weather.Length > 0 && WeatherInfo.Weather[0]?.IconUrl != null
            ? WeatherInfo.Weather[0].IconUrl! 
            : string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasCustomApiKey))]
        public partial string? SavedApiKey { get; set; }

        [ObservableProperty]
        public partial string? SelectedUnit { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayTemp))]
        [NotifyPropertyChangedFor(nameof(DisplayFeelsLike))]
        [NotifyPropertyChangedFor(nameof(DisplayWind))]
        [NotifyPropertyChangedFor(nameof(TempSymbol))]
        [NotifyPropertyChangedFor(nameof(SpeedSymbol))]
        public partial bool IsMetric { get; set; }

        [ObservableProperty]
        public partial bool IsSettingsVisible { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWelcomeVisible))]
        public partial bool IsWeatherVisible { get; set; }

        public bool IsWelcomeVisible => !IsWeatherVisible && !IsLoading && !HasError;

        public bool HasCustomApiKey => !string.IsNullOrWhiteSpace(SavedApiKey);

        public string TempSymbol => IsMetric ? "°C" : "°F";
        public string SpeedSymbol => IsMetric ? "m/s" : "mph";

        [RelayCommand]
        public async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(CityQuery))
            {
                HasError = true;
                ErrorMessage = "Please enter a city name.";
                IsWeatherVisible = false;
                return;
            }

            IsLoading = true;
            HasError = false;
            ErrorMessage = string.Empty;
            IsWeatherVisible = false;

            try
            {
                // A custom OpenWeatherMap key is optional; the service falls back to a keyless provider.
                var activeApiKey = !string.IsNullOrWhiteSpace(SavedApiKey) ? SavedApiKey : string.Empty;
                var unitSystem = IsMetric ? "metric" : "imperial";

                var result = await _weatherService.GetWeatherAsync(CityQuery.Trim(), unitSystem, activeApiKey);

                if (result.Cod == 200)
                {
                    WeatherInfo = result;
                    IsWeatherVisible = true;
                }
                else
                {
                    HasError = true;
                    ErrorMessage = result.Message ?? "Failed to fetch weather. Please try again.";
                }
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"An unexpected error occurred: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task SaveApiKeyAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SavedApiKey))
                {
                    await SecureStorage.Default.SetAsync("WeatherApiKey", string.Empty);
                    SavedApiKey = string.Empty;
                }
                else
                {
                    await SecureStorage.Default.SetAsync("WeatherApiKey", SavedApiKey.Trim());
                }
                
                OnPropertyChanged(nameof(HasCustomApiKey));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to save API key securely: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task ClearApiKeyAsync()
        {
            try
            {
                SecureStorage.Default.Remove("WeatherApiKey");
                SavedApiKey = string.Empty;
                OnPropertyChanged(nameof(HasCustomApiKey));
            }
            catch (Exception ex)
            {
                HasError = true;
                ErrorMessage = $"Failed to clear API key securely: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ToggleUnits()
        {
            IsMetric = !IsMetric;
            SelectedUnit = IsMetric ? "metric" : "imperial";
            
            OnPropertyChanged(nameof(TempSymbol));
            OnPropertyChanged(nameof(SpeedSymbol));

            // Refresh the weather search if a city is already displayed
            if (WeatherInfo != null && IsWeatherVisible)
            {
                _ = SearchAsync();
            }
        }

        [RelayCommand]
        public void ToggleSettings()
        {
            IsSettingsVisible = !IsSettingsVisible;
        }

        private async Task LoadApiKeyAsync()
        {
            try
            {
                var key = await SecureStorage.Default.GetAsync("WeatherApiKey");
                if (!string.IsNullOrEmpty(key))
                {
                    SavedApiKey = key;
                }
            }
            catch
            {
                // Fallback for tests/environments where SecureStorage is not available
                SavedApiKey = string.Empty;
            }
        }
    }
}
