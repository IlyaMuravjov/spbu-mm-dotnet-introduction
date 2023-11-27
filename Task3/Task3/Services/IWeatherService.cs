namespace Task3.Services
{
    public interface IWeatherService
    {
        string Name { get; }
        Task<WeatherData> GetCurrentWeatherAsync();
    }
}
