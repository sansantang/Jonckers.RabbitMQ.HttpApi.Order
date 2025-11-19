using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Core.IService
{
    /// <summary>
    /// 用于注入使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMyPublisher<T> where T : class
    {
        Task PublishAsync(T data, Encoding encoding = null);
        Task PublishAsync(string routingKey, T data, string exchangeName = "", Encoding encoding = null);
    }
}
