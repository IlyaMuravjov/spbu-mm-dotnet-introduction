namespace Task3.Services
{
    public class EnvironmentVariablesBasedTask3Settings : ITask3Settings
    {
        public double Latitude { get; } = double.Parse(GetEnvironmentVariable("TASK3_LATITUDE"));
        public double Longitude { get; } = double.Parse(GetEnvironmentVariable("TASK3_LONGITUDE"));
        public string TomorrowIoApiKey => GetEnvironmentVariable("TASK3_TOMORROW_IO_API_KEY");
        public string OpenWeatherMapApiKey => GetEnvironmentVariable("TASK3_OPEN_WEATHER_MAP_API_KEY");

        private static string GetEnvironmentVariable(string variableName) =>
            Environment.GetEnvironmentVariable(variableName) 
                ?? throw new InvalidOperationException($"Environment variable '{variableName}' is not set.");
    }
}
