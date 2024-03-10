using Microsoft.AspNetCore.Mvc;
using Moq;
using Task3.Controllers;
using Task3.Services;
using Task3;
using Microsoft.Extensions.Logging;

namespace Test
{
    [TestClass]
    public class WeatherControllerTests
    {
        // initialized in [TestInitialize]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        private WeatherController _controller;
        private Mock<IWeatherService> _serviceWithPartialData;
        private Mock<IWeatherService> _serviceThatCrashes;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [TestInitialize]
        public void Initialize()
        {
            _serviceWithPartialData = new Mock<IWeatherService>();
            _serviceWithPartialData.Setup(s => s.GetCurrentWeatherAsync()).ReturnsAsync(new WeatherData(
                TemperatureC: -5.3,
                CloudCover: 30,
                Humidity: 70,
                RainMmPerHr: 5.2,
                FreezingRainMmPerHr: null,
                SnowMmPerHr: 0,
                WindDirectionDeg: 180,
                WindSpeedMPerSec: 5
            ));
            _serviceWithPartialData.Setup(s => s.Name).Returns("PartialService");

            _serviceThatCrashes = new Mock<IWeatherService>();
            _serviceThatCrashes.Setup(s => s.GetCurrentWeatherAsync()).ThrowsAsync(new System.Exception("Service failure"));
            _serviceThatCrashes.Setup(s => s.Name).Returns("CrashingService");

            var loggerMock = new Mock<ILogger<WeatherController>>();
            var weatherServices = new List<IWeatherService> { _serviceWithPartialData.Object, _serviceThatCrashes.Object };
            _controller = new WeatherController(loggerMock.Object, weatherServices);
        }

        [TestMethod]
        public void WhenThereAreTwoServicesThenGetServicesShouldReturnTwoServiceNames()
        {
            var result = _controller.GetServices();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = (OkObjectResult) result;
            var services = okResult.Value as List<string>;
            Assert.IsNotNull(services);
            Assert.AreEqual(2, services.Count);
            Assert.IsTrue(services.Contains("PartialService"));
            Assert.IsTrue(services.Contains("CrashingService"));
        }

        [TestMethod]
        public async Task WhenRequestedServiceReturnsPartialDataThenGetWeatherAsyncForSpecificServiceShouldHandleMissingFields()
        {
            var result = await _controller.GetWeatherAsync("PartialService");

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = (OkObjectResult) result;
            var weatherResponse = okResult.Value as WeatherResponse;
            Assert.IsNotNull(weatherResponse);
            Assert.AreEqual("PartialService", weatherResponse.Service);
            Assert.AreEqual("-5.3", weatherResponse.TemperatureC);
            Assert.AreEqual("22.46", weatherResponse.TemperatureF);
            Assert.AreEqual("30", weatherResponse.CloudCover);
            Assert.AreEqual("70", weatherResponse.Humidity);
            Assert.AreEqual("5.2", weatherResponse.RainMmPerHr);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, weatherResponse.FreezingRainMmPerHr);
            Assert.AreEqual("0", weatherResponse.SnowMmPerHr);
            Assert.AreEqual("180", weatherResponse.WindDirectionDeg);
            Assert.AreEqual("5", weatherResponse.WindSpeedMPerSec);
        }

        [TestMethod]
        public async Task WhenOneServiceReturnsPartialDataAndAnotherOneFailsThenGetWeatherAsyncForAllServicesShouldHandleMissingFieldsAndFailure()
        {
            var result = await _controller.GetWeatherAsync();

            Assert.IsInstanceOfType(result, typeof(OkObjectResult));
            var okResult = (OkObjectResult)result;
            var weatherResponses = okResult.Value as IEnumerable<WeatherResponse>;
            Assert.IsNotNull(weatherResponses);

            var partialServiceResponse = weatherResponses.First(r => r.Service == "PartialService");
            Assert.AreEqual("-5.3", partialServiceResponse.TemperatureC);
            Assert.AreEqual("22.46", partialServiceResponse.TemperatureF);
            Assert.AreEqual("30", partialServiceResponse.CloudCover);
            Assert.AreEqual("70", partialServiceResponse.Humidity);
            Assert.AreEqual("5.2", partialServiceResponse.RainMmPerHr);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, partialServiceResponse.FreezingRainMmPerHr);
            Assert.AreEqual("0", partialServiceResponse.SnowMmPerHr);
            Assert.AreEqual("180", partialServiceResponse.WindDirectionDeg);
            Assert.AreEqual("5", partialServiceResponse.WindSpeedMPerSec);

            var crashingServiceResponse = weatherResponses.First(r => r.Service == "CrashingService");
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.TemperatureC);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.TemperatureF);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.CloudCover);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.Humidity);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.RainMmPerHr);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.FreezingRainMmPerHr);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.SnowMmPerHr);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.WindDirectionDeg);
            Assert.AreEqual(WeatherResponse.NO_DATA_MESSAGE, crashingServiceResponse.WindSpeedMPerSec);
        }
    }
}
