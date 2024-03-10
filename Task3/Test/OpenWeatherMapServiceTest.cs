using System;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Task3.Services;

namespace Test
{
    [TestClass]
    public class OpenWeatherMapServiceTest
    {
        // initialized in [TestInitialize]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private OpenWeatherMapService _openWeatherMapService;
        private Mock<HttpClient> _httpClientMock;
        private Mock<ITask3Settings> _settingsMock;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            _httpClientMock = new Mock<HttpClient>();
            _settingsMock = new Mock<ITask3Settings>();
            _openWeatherMapService = new OpenWeatherMapService(_httpClientMock.Object, _settingsMock.Object);

            _settingsMock.Setup(gp => gp.Latitude).Returns(12.345);
            _settingsMock.Setup(gp => gp.Longitude).Returns(67.89);
            _settingsMock.Setup(gp => gp.OpenWeatherMapApiKey).Returns("987654321abc");
        }

        [TestMethod]
        public async Task WhenApiReturnsValidDataIncludingRainAndSnowThenGetCurrentWeatherAsyncShouldReturnWeatherDataWithRainAndSnow()
        {
            var jsonSampleResponse = @"
            {
                ""main"": {
                    ""temp"": 25.5,
                    ""humidity"": 65,
                    ""unused"": ""lalala""
                },
                ""wind"": {
                    ""speed"": 5.8,
                    ""deg"": 180
                },
                ""clouds"": {
                    ""all"": 40
                },
                ""rain"": {
                    ""1h"": 2.5,
                    ""3h"": 8.0
                },
                ""snow"": {
                    ""1h"": 1.0
                }
            }";

            _httpClientMock.Setup(
               client => client.SendAsync(
                    It.Is<HttpRequestMessage>(request => IsExpectedRequest(request)),
                    It.IsAny<CancellationToken>()
               )
            ).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(jsonSampleResponse),
            });

            var weatherData = await _openWeatherMapService.GetCurrentWeatherAsync();

            Assert.IsNotNull(weatherData);
            Assert.AreEqual(25.5, weatherData.TemperatureC);
            Assert.AreEqual(77.9, weatherData.TemperatureF);
            Assert.AreEqual(65, weatherData.Humidity);
            Assert.AreEqual(5.8, weatherData.WindSpeedMPerSec);
            Assert.AreEqual(180, weatherData.WindDirectionDeg);
            Assert.AreEqual(40, weatherData.CloudCover);
            Assert.AreEqual(2.5, weatherData.RainMmPerHr);
            Assert.AreEqual(1.0, weatherData.SnowMmPerHr);
            Assert.IsNull(weatherData.FreezingRainMmPerHr);
        }

        [TestMethod]
        public async Task WhenApiReturnsValidDataWithoutRainAndSnowThenGetCurrentWeatherAsyncShouldReturnWeatherDataWithoutRainAndSnow()
        {
            var jsonSampleResponse = @"
            {
                ""main"": {
                    ""temp"": 20.0,
                    ""humidity"": 70
                },
                ""wind"": {
                    ""speed"": 4.5,
                    ""deg"": 270
                },
                ""clouds"": {
                    ""all"": 20
                }
            }";

            _httpClientMock.Setup(
               client => client.SendAsync(
                    It.Is<HttpRequestMessage>(request => IsExpectedRequest(request)),
                    It.IsAny<CancellationToken>()
                )
            ).ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = System.Net.HttpStatusCode.OK,
                Content = new StringContent(jsonSampleResponse),
            });

            var weatherData = await _openWeatherMapService.GetCurrentWeatherAsync();

            Assert.IsNotNull(weatherData);
            Assert.AreEqual(20.0, weatherData.TemperatureC);
            Assert.AreEqual(68.0, weatherData.TemperatureF);
            Assert.AreEqual(70, weatherData.Humidity);
            Assert.AreEqual(4.5, weatherData.WindSpeedMPerSec);
            Assert.AreEqual(270, weatherData.WindDirectionDeg);
            Assert.AreEqual(20, weatherData.CloudCover);
            Assert.IsNull(weatherData.RainMmPerHr);
            Assert.IsNull(weatherData.SnowMmPerHr);
            Assert.IsNull(weatherData.FreezingRainMmPerHr);
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
                () => _openWeatherMapService.GetCurrentWeatherAsync()
            );
        }

        private static bool IsExpectedRequest(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get || !request.RequestUri!.ToString().StartsWith("https://api.openweathermap.org/data/2.5/weather"))
            {
                return false;
            }

            var query = HttpUtility.ParseQueryString(request.RequestUri.Query);
            return query["lat"] == "12.345"
                && query["lon"] == "67.89"
                && query["units"] == "metric"
                && query["appid"] == "987654321abc";
        }
    }
}
