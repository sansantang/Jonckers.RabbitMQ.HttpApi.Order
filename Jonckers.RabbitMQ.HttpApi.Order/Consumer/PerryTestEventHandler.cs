using Jonckers.RabbitMQ.Service.ConsumerMessageModel;
using Jonckers.RabbitMQClient.Core.Service;

namespace Jonckers.RabbitMQ.HttpApi.Order.Consumer
{
    public class PerryTestEventHandler : MyEventHandler<PerryTest>
    {
        public PerryTestEventHandler()
        {
            Options.PrefetchSize = 0;
            // 配置 QoS 参数
            Options.PrefetchCount = 2;    // 每次处理2条消息
            Options.Global = false;
        }

        public override Task OnReceivedAsync(PerryTest data, string message)
        {
            Console.WriteLine(message);
            return Task.CompletedTask;
        }

        public override void OnConsumerException(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
