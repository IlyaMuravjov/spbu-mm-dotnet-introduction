using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using Task3.Services;

namespace Task3.Controllers
{
    [ApiController]
    [Route("weather")]
    public class WeatherController(
        ILogger<WeatherController> logger,
        IEnumerable<IWeatherService> weatherServices
    ) : ControllerBase
    {
        private readonly ILogger<WeatherController> _logger = logger;
        private readonly Dictionary<string, IWeatherService> _weatherServices = weatherServices.ToDictionary(service => service.Name);

        [HttpGet("services")]
        public IActionResult GetServices() => Ok(_weatherServices.Keys.ToList());

        [HttpGet]
        public async Task<IActionResult> GetWeatherAsync() =>
            Ok(await Task.WhenAll(_weatherServices.Values.Select(FetchWeatherAsync)));

        [HttpGet("{serviceName}")]
        public async Task<IActionResult> GetWeatherAsync(string serviceName)
        {
            if (_weatherServices.TryGetValue(serviceName, out var service))
            {
                return Ok(await FetchWeatherAsync(service));
            }
            else
            {
                return NotFound($"Service not found {service}");
            }
        }

        private async Task<WeatherResponse> FetchWeatherAsync(IWeatherService service)
        {
            try
            {
                var weatherData = await service.GetCurrentWeatherAsync();
                return new WeatherResponse(service.Name, weatherData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weather data from {service}", service.Name);
                return new WeatherResponse(service.Name, null);
            }
        }
    }

    public class WeatherResponse(String service, WeatherData? data)
    {
        public string Service { get; } = service;
        public string TemperatureC { get; } = FormatNullableData(data?.TemperatureC);
        public string TemperatureF { get; } = FormatNullableData(data?.TemperatureF);
        public string CloudCover { get; } = FormatNullableData(data?.CloudCover);
        public string Humidity { get; } = FormatNullableData(data?.Humidity);
        public string RainMmPerHr { get; } = FormatNullableData(data?.RainMmPerHr);
        public string FreezingRainMmPerHr { get; } = FormatNullableData(data?.FreezingRainMmPerHr);
        public string SnowMmPerHr { get; } = FormatNullableData(data?.SnowMmPerHr);
        public string WindDirectionDeg { get; } = FormatNullableData(data?.WindDirectionDeg);
        public string WindSpeedMPerSec { get; } = FormatNullableData(data?.WindSpeedMPerSec);

        public const string NO_DATA_MESSAGE = "Данных нет";

        private static string FormatNullableData(double? data)
        {
            return data.HasValue ? data.Value.ToString(CultureInfo.InvariantCulture) : NO_DATA_MESSAGE;
        }
    }
}
