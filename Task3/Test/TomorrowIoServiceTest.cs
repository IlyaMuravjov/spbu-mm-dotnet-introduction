using System.Web;
using Moq;
using Task3.Services;

namespace Test
{
    [TestClass]
    public class TomorrowIoServiceTests
    {
        // initialized in [TestInitialize]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private TomorrowIoService _tomorrowIoService;
        private Mock<HttpClient> _httpClientMock;
        private Mock<ITask3Settings> _settingsMock;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            _httpClientMock = new Mock<HttpClient>();
            _settingsMock = new Mock<ITask3Settings>();
            _tomorrowIoService = new TomorrowIoService(_httpClientMock.Object, _settingsMock.Object);

            _settingsMock.Setup(gp => gp.Latitude).Returns(12.345);
            _settingsMock.Setup(gp => gp.Longitude).Returns(67.89);
            _settingsMock.Setup(gp => gp.TomorrowIoApiKey).Returns("01234567890abcdef");
        }

        [TestMethod]
        public async Task WhenApiReturnsValidDataThenGetCurrentWeatherAsyncShouldReturnWeatherData()
        {

            var jsonSampleResponse = @"
            {
                ""data"": {
                    ""values"": {
                        ""temperature"": -5.3,
                        ""cloudCover"": 30.0,
                        ""humidity"": 70.0,
                        ""rainIntensity"": 5.2,
                        ""freezingRainIntensity"": 0.1,
                        ""snowIntensity"": 0.0,
                        ""windDirection"": 180.0,
                        ""windSpeed"": 5.0,
                        ""someExtradField"": 123.45
                    },
                    ""anotherUnusedField"": ""text""
                }
            }";

            _httpClientMock.Setup(
               client => client.SendAsync(
                    It.Is<HttpRequestMessage>(request => IsExpectedRequest(request)),
                    It.IsAny<CancellationToken>()
            )).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(jsonSampleResponse),
            });

            var weatherData = await _tomorrowIoService.GetCurrentWeatherAsync();

            Assert.IsNotNull(weatherData);
            Assert.AreEqual(-5.3, weatherData.TemperatureC);
            Assert.AreEqual(22.46, weatherData.TemperatureF);
            Assert.AreEqual(30.0, weatherData.CloudCover);
            Assert.AreEqual(70.0, weatherData.Humidity);
            Assert.AreEqual(5.2, weatherData.RainMmPerHr);
            Assert.AreEqual(0.1, weatherData.FreezingRainMmPerHr);
            Assert.AreEqual(0.0, weatherData.SnowMmPerHr);
            Assert.AreEqual(180.0, weatherData.WindDirectionDeg);
            Assert.AreEqual(5.0, weatherData.WindSpeedMPerSec);
        }

        [TestMethod]
        public async Task WhenApiReturnsErrorThenGetCurrentWeatherAsyncShouldHandleApiFailure()
        {
            _httpClientMock.Setup(
                client => client.SendAsync(
                    It.Is<HttpRequestMessage>(request => IsExpectedRequest(request)),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.Unauthorized,
            });

            await Assert.ThrowsExceptionAsync<HttpRequestException>(
                () => _tomorrowIoService.GetCurrentWeatherAsync()
            );
        }

        private static bool IsExpectedRequest(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get || !request.RequestUri!.ToString().StartsWith("https://api.tomorrow.io/v4/weather/realtime"))
            {
                return false;
            }

            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            return query["location"] == "12.345, 67.89" 
                && query["units"] == "metric" 
                && query["apikey"] == "01234567890abcdef";
        }
    }
}
