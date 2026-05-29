using System.Threading.Tasks;
using WeatherApp.Models;

namespace WeatherApp.Services
{
    public interface IWeatherService
    {
        Task<WeatherResponse> GetWeatherAsync(string city, string units, string apiKey);
    }
}
