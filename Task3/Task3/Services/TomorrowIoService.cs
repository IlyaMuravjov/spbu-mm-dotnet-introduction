using System.Globalization;
using System.Text.Json;
using System.Web;

namespace Task3.Services
{
    public class TomorrowIoService(HttpClient httpClient, ITask3Settings settings) : IWeatherService
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly ITask3Settings _settings = settings;
        
        public string Name => "tomorrow.io";

        public async Task<WeatherData> GetCurrentWeatherAsync()
        {
            var parameters = HttpUtility.ParseQueryString(string.Empty);
            parameters["location"] = $"{_settings.Latitude.ToString(CultureInfo.InvariantCulture)}, " +
                $"{_settings.Longitude.ToString(CultureInfo.InvariantCulture)}";
            parameters["units"] = "metric";
            parameters["apikey"] = _settings.TomorrowIoApiKey;

            UriBuilder builder = new("https://api.tomorrow.io/v4/weather/realtime")
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
            var values = jsonDoc.RootElement.GetProperty("data").GetProperty("values");

            return new WeatherData(
                TemperatureC: values.GetProperty("temperature").GetDouble(),
                CloudCover: values.GetProperty("cloudCover").GetDouble(),
                Humidity: values.GetProperty("humidity").GetDouble(),
                RainMmPerHr: values.GetProperty("rainIntensity").GetDouble(),
                FreezingRainMmPerHr: values.GetProperty("freezingRainIntensity").GetDouble(),
                SnowMmPerHr: values.GetProperty("snowIntensity").GetDouble(),
                WindDirectionDeg: values.GetProperty("windDirection").GetDouble(),
                WindSpeedMPerSec: values.GetProperty("windSpeed").GetDouble()
            );
        }
    }
}
