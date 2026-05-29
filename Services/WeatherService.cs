using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

        public WeatherService(HttpClient? httpClient = null)
        {
            _httpClient = httpClient ?? new HttpClient();
        }

        public async Task<WeatherResponse> GetWeatherAsync(string city, string units, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(city))
                throw new ArgumentException("City name cannot be empty.", nameof(city));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API key cannot be empty. Please configure it in settings.", nameof(apiKey));

            var url = $"{BaseUrl}?q={Uri.EscapeDataString(city)}&units={units}&appid={apiKey}";

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
                            Message = "Invalid API Key. Please verify your OpenWeatherMap API key in settings."
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
    }
}
