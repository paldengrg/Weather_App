using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private const string OpenWeatherBaseUrl = "https://api.openweathermap.org/data/2.5/weather";
        private const string OpenMeteoGeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
        private const string OpenMeteoForecastUrl = "https://api.open-meteo.com/v1/forecast";

        public WeatherService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<WeatherResponse> GetWeatherAsync(string city, string units, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City name cannot be empty.", nameof(city));

            if (string.IsNullOrWhiteSpace(apiKey))
                return await GetOpenMeteoWeatherAsync(city, units);

            var openWeatherResult = await GetOpenWeatherAsync(city, units, apiKey);
            return openWeatherResult.Cod == (int)HttpStatusCode.Unauthorized
                ? await GetOpenMeteoWeatherAsync(city, units)
                : openWeatherResult;
        }

        private async Task<WeatherResponse> GetOpenWeatherAsync(string city, string units, string apiKey)
        {
            var url = $"{OpenWeatherBaseUrl}?q={Uri.EscapeDataString(city)}&units={units}&appid={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var data = JsonSerializer.Deserialize<WeatherResponse>(content);
                    if (data != null)
                    {
                        data.Cod = (int)response.StatusCode;
                        return data;
                    }
                    throw new Exception("Failed to deserialize weather data.");
                }
                else
                {
                    // Attempt to parse the error message from API
                    try
                    {
                        var errorData = JsonSerializer.Deserialize<WeatherResponse>(content);
                        if (errorData != null && !string.IsNullOrEmpty(errorData.Message))
                        {
                            return new WeatherResponse
                            {
                                Cod = (int)response.StatusCode,
                                Message = errorData.Message
                            };
                        }
                    }
                    catch
                    {
                        // Ignore parsing error and fallback to standard message
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        return new WeatherResponse
                        {
                            Cod = (int)response.StatusCode,
                            Message = "Invalid OpenWeatherMap API key."
                        };
                    }

                    return new WeatherResponse
                    {
                        Cod = (int)response.StatusCode,
                        Message = $"Error: {response.ReasonPhrase} (Status {(int)response.StatusCode})"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                return new WeatherResponse
                {
                    Cod = 0,
                    Message = $"Network error: Please check your internet connection. Details: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new WeatherResponse
                {
                    Cod = 0,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        private async Task<WeatherResponse> GetOpenMeteoWeatherAsync(string city, string units)
        {
            try
            {
                var geocodeUrl = $"{OpenMeteoGeocodingUrl}?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";
                var geocodeResponse = await _httpClient.GetAsync(geocodeUrl);
                var geocodeContent = await geocodeResponse.Content.ReadAsStringAsync();

                if (!geocodeResponse.IsSuccessStatusCode)
                {
                    return new WeatherResponse
                    {
                        Cod = (int)geocodeResponse.StatusCode,
                        Message = $"Location lookup failed: {geocodeResponse.ReasonPhrase}"
                    };
                }

                var geocode = JsonSerializer.Deserialize<OpenMeteoGeocodingResponse>(geocodeContent);
                var location = geocode?.Results is { Length: > 0 } ? geocode.Results[0] : null;

                if (location == null)
                {
                    return new WeatherResponse
                    {
                        Cod = (int)HttpStatusCode.NotFound,
                        Message = "city not found"
                    };
                }

                var tempUnit = units == "imperial" ? "&temperature_unit=fahrenheit" : string.Empty;
                var windUnit = units == "imperial" ? "mph" : "ms";
                var latitude = location.Latitude.ToString(CultureInfo.InvariantCulture);
                var longitude = location.Longitude.ToString(CultureInfo.InvariantCulture);
                var forecastUrl = $"{OpenMeteoForecastUrl}?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,pressure_msl,wind_speed_10m{tempUnit}&wind_speed_unit={windUnit}";
                var forecastResponse = await _httpClient.GetAsync(forecastUrl);
                var forecastContent = await forecastResponse.Content.ReadAsStringAsync();

                if (!forecastResponse.IsSuccessStatusCode)
                {
                    return new WeatherResponse
                    {
                        Cod = (int)forecastResponse.StatusCode,
                        Message = $"Weather lookup failed: {forecastResponse.ReasonPhrase}"
                    };
                }

                var forecast = JsonSerializer.Deserialize<OpenMeteoForecastResponse>(forecastContent);
                if (forecast?.Current == null)
                {
                    return new WeatherResponse
                    {
                        Cod = 0,
                        Message = "Failed to read weather data."
                    };
                }

                var (description, icon) = GetWeatherDescription(forecast.Current.WeatherCode);
                return new WeatherResponse
                {
                    Cod = (int)HttpStatusCode.OK,
                    Name = location.Name,
                    Sys = new Sys { Country = location.CountryCode },
                    Main = new MainData
                    {
                        Temp = forecast.Current.Temperature,
                        FeelsLike = forecast.Current.ApparentTemperature,
                        Humidity = forecast.Current.RelativeHumidity,
                        Pressure = (int)Math.Round(forecast.Current.Pressure)
                    },
                    Wind = new Wind
                    {
                        Speed = forecast.Current.WindSpeed
                    },
                    Weather = new[]
                    {
                        new WeatherItem
                        {
                            Description = description,
                            Icon = icon
                        }
                    }
                };
            }
            catch (HttpRequestException ex)
            {
                return new WeatherResponse
                {
                    Cod = 0,
                    Message = $"Network error: Please check your internet connection. Details: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                return new WeatherResponse
                {
                    Cod = 0,
                    Message = $"Unexpected error: {ex.Message}"
                };
            }
        }

        private static (string Description, string Icon) GetWeatherDescription(int weatherCode)
        {
            return weatherCode switch
            {
                0 => ("clear sky", "01d"),
                1 or 2 => ("partly cloudy", "02d"),
                3 => ("overcast clouds", "04d"),
                45 or 48 => ("fog", "50d"),
                51 or 53 or 55 => ("drizzle", "09d"),
                56 or 57 => ("freezing drizzle", "09d"),
                61 or 63 or 65 => ("rain", "10d"),
                66 or 67 => ("freezing rain", "13d"),
                71 or 73 or 75 or 77 => ("snow", "13d"),
                80 or 81 or 82 => ("rain showers", "09d"),
                85 or 86 => ("snow showers", "13d"),
                95 or 96 or 99 => ("thunderstorm", "11d"),
                _ => ("current weather", "02d")
            };
        }

        private sealed class OpenMeteoGeocodingResponse
        {
            [JsonPropertyName("results")]
            public OpenMeteoLocation[]? Results { get; set; }
        }

        private sealed class OpenMeteoLocation
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("latitude")]
            public double Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public double Longitude { get; set; }

            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }
        }

        private sealed class OpenMeteoForecastResponse
        {
            [JsonPropertyName("current")]
            public OpenMeteoCurrentWeather? Current { get; set; }
        }

        private sealed class OpenMeteoCurrentWeather
        {
            [JsonPropertyName("temperature_2m")]
            public double Temperature { get; set; }

            [JsonPropertyName("relative_humidity_2m")]
            public int RelativeHumidity { get; set; }

            [JsonPropertyName("apparent_temperature")]
            public double ApparentTemperature { get; set; }

            [JsonPropertyName("weather_code")]
            public int WeatherCode { get; set; }

            [JsonPropertyName("pressure_msl")]
            public double Pressure { get; set; }

            [JsonPropertyName("wind_speed_10m")]
            public double WindSpeed { get; set; }
        }
    }
}
