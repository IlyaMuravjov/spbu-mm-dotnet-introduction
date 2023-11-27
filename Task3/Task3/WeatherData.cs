namespace Task3
{
    public record WeatherData(
        double TemperatureC,
        double CloudCover,
        double Humidity,
        double? RainMmPerHr,
        double? FreezingRainMmPerHr,
        double? SnowMmPerHr,
        double WindDirectionDeg,
        double WindSpeedMPerSec)
    {
        public double TemperatureF => 32 + (TemperatureC * 9.0 / 5.0);
    }
}
