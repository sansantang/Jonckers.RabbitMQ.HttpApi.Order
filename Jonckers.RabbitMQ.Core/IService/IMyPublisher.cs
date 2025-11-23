using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQClient.Core.IService
{
    /// <summary>
    /// 用于注入使用
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IMyPublisher<T> where T : class
    {
        Task PublishAsync(T data, Encoding encoding = null);

        Task PublishAsync(T data, string expiration, Encoding encoding = null);
        Task PublishWithDeadLetterAsync(T data, Encoding encoding = null);
    }
}
