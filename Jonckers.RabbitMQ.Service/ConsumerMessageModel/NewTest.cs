using Jonckers.RabbitMQ.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Service.ConsumerMessageModel
{
    [RabbitMQEvent(queue: "jonckers.enterpriseordering.newtest",routingkey: "jonckers.enterpriseordering.newtest")]
    public class NewTest
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public int Count { get; set; }
        public string? Remark { get; set; }
    }
}
