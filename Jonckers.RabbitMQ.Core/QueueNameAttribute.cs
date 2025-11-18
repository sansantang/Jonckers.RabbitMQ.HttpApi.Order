using System;
using System.Collections.Generic;
using System.Text;

namespace Jonckers.RabbitMQ.Core
{
    /// <summary>
    /// 定义队列名字，优先级高于类完整名
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class QueueNameAttribute : Attribute
    {
        public string QueueName { get; }
        public QueueNameAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
