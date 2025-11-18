using Jonckers.RabbitMQ.Core.Service;

namespace Jonckers.RabbitMQ.HttpApi.Order.Consumer
{
    public class PerryTestEventHandler : MyEventHandler<PerryTest>
    {
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
