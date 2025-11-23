using Jonckers.RabbitMQClient.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Service.ConsumerMessageModel
{
    [RabbitMQEvent(queue: "jonckers.enterpriseordering.deadletter", routingkey: "jonckers.enterpriseordering.deadletter", exchange: "jonckers.enterpriseordering", isWithDeadLetter: true, expirationMilliseconds: 60000)]
    public class DeadLetterTest
    {
        public Guid Id { get; set; }
        public string? Status { get; set; }
        public int Count { get; set; }
        public string? Remark { get; set; }
    }
}
