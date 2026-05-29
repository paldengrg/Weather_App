using System.Text.Json.Serialization;

namespace WeatherApp.Models
{
    public class WeatherResponse
    {
        [JsonPropertyName("coord")]
        public Coord? Coord { get; set; }

        [JsonPropertyName("weather")]
        public WeatherItem[]? Weather { get; set; }

        [JsonPropertyName("main")]
        public MainData? Main { get; set; }

        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }

        [JsonPropertyName("wind")]
        public Wind? Wind { get; set; }

        [JsonPropertyName("sys")]
        public Sys? Sys { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("cod")]
        public int Cod { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    public class Coord
    {
        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }
    }

    public class WeatherItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("main")]
        public string? Main { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("icon")]
        public string? Icon { get; set; }

        public string IconUrl => !string.IsNullOrEmpty(Icon) ? $"https://openweathermap.org/img/wn/{Icon}@2x.png" : string.Empty;
    }

    public class MainData
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("temp_min")]
        public double TempMin { get; set; }

        [JsonPropertyName("temp_max")]
        public double TempMax { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }
    }

    public class Wind
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("deg")]
        public int Deg { get; set; }
    }

    public class Sys
    {
        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("sunrise")]
        public long Sunrise { get; set; }

        [JsonPropertyName("sunset")]
        public long Sunset { get; set; }
    }
}
