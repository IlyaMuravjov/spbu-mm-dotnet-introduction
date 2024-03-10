namespace Task3.Services
{
    public interface ITask3Settings
    {
        public double Latitude { get; }
        public double Longitude { get; }
        public string TomorrowIoApiKey { get; }
        public string OpenWeatherMapApiKey { get; }
    }
}
