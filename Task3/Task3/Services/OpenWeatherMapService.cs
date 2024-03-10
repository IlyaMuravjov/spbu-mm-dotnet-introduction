using System.Globalization;
using System.Text.Json;
using System.Web;

namespace Task3.Services
{
    public class OpenWeatherMapService(HttpClient httpClient, ITask3Settings settings) : IWeatherService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ITask3Settings _settings = settings;

        public string Name => "openweathermap.org";
        
        public async Task<WeatherData> GetCurrentWeatherAsync()
        {
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["lat"] = _settings.Latitude.ToString(CultureInfo.InvariantCulture);
            parameters["lon"] = _settings.Longitude.ToString(CultureInfo.InvariantCulture);
            parameters["units"] = "metric";
            parameters["appid"] = _settings.OpenWeatherMapApiKey;

            UriBuilder builder = new("https://api.openweathermap.org/data/2.5/weather")
            {
                Query = parameters.ToString()
            };

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = builder.Uri,
                Headers = { { "accept", "application/json" } },
            };

            string responseBody;
            using (var response = await _httpClient.SendAsync(request, CancellationToken.None))
            {
                response.EnsureSuccessStatusCode();
                responseBody = await response.Content.ReadAsStringAsync();
            }

            var jsonDoc = JsonDocument.Parse(responseBody);
            var main = jsonDoc.RootElement.GetProperty("main");
            var wind = jsonDoc.RootElement.GetProperty("wind");
            var clouds = jsonDoc.RootElement.GetProperty("clouds");

            return new WeatherData(
                TemperatureC: main.GetProperty("temp").GetDouble(),
                CloudCover: clouds.GetProperty("all").GetDouble(),
                Humidity: main.GetProperty("humidity").GetDouble(),
                // OpenWeatherMap optionally provides rainMmPerHr
                RainMmPerHr: jsonDoc.RootElement.TryGetProperty("rain", out var rain) ? rain.GetProperty("1h").GetDouble() : null,
                // OpenWeatherMap never provides freezingRainMmPerHr
                FreezingRainMmPerHr: null,
                // OpenWeatherMap optionally provides snowMmPerHr
                SnowMmPerHr: jsonDoc.RootElement.TryGetProperty("snow", out var snow) ? snow.GetProperty("1h").GetDouble() : null,
                WindDirectionDeg: wind.GetProperty("deg").GetDouble(),
                WindSpeedMPerSec: wind.GetProperty("speed").GetDouble()
            );
        }
    }
}
