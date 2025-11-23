using Jonckers.RabbitMQ.Service.ConsumerMessageModel;
using Jonckers.RabbitMQClient.Core.IService;
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

        [HttpGet("test")]
        public async Task<string> TestAsync()
        {
            var data = new PerryTest()
            {
                Id = Guid.NewGuid(),
                Name = "Pending",
                Count = 123,
                Remark = "哈哈哈"
            };

            await TestPublisher.PublishAsync(data);

            return "发送了一个消息";
        }
    }
}
