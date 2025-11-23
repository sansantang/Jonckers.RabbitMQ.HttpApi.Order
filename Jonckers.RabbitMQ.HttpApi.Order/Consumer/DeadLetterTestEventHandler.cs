using Jonckers.RabbitMQ.Core.Service;
using Jonckers.RabbitMQ.Service.ConsumerMessageModel;

namespace Jonckers.RabbitMQ.HttpApi.Order.Consumer
{
    public class DeadLetterTestEventHandler : MyEventHandler<DeadLetterTest>
    {
        public override Task OnReceivedAsync(DeadLetterTest data, string message)
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
