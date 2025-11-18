using System;

namespace Jonckers.RabbitMQ.Core.Options
{
    public class RabbitMQOptions
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string ExchangeName { get; set; } = "";
    }
}
