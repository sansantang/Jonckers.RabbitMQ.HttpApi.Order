using Jonckers.RabbitMQ.Core.Service;
using Jonckers.RabbitMQ.Service.ConsumerMessageModel;
using Microsoft.Extensions.Options;

namespace Jonckers.RabbitMQ.HttpApi.Order.Consumer
{
    public class NewTestEventHandler : MyEventHandler<NewTest>
    {
        public override Task OnReceivedAsync(NewTest data, string message)
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
