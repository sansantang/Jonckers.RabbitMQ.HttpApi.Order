using Jonckers.RabbitMQ.Core.IService;
using Microsoft.AspNetCore.Mvc;

namespace Jonckers.RabbitMQ.HttpApi.Order.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {

        public IMyPublisher<PerryTest> TestPublisher { get; }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IMyPublisher<PerryTest> testPublisher)
        {
            _logger = logger;
            TestPublisher = testPublisher;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }


        [HttpGet("test")]
        public async Task<string> TestAsync()
        {
            var data = new PerryTest()
            {
                Id = Guid.NewGuid(),
                Name = "AAA",
                Count = 123,
                Remark = "哈哈哈"
            };

            //await TestPublisher.PublishAsync(data);
            await TestPublisher.PublishAsync("jonckers.enterpriseordering.requestevent", data);

            return "发送了一个消息";
        }
    }
}
