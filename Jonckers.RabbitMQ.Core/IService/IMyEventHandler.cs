using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQClient.Core.IService
{
    /// <summary>
    /// 用于注入使用
    /// </summary>
    public interface IMyEventHandler<T> : IMyEventHandler where T : class 
    {
        //Task PublishAsync(T data, Encoding encoding = null);
        Task OnReceivedAsync(T data,  string message);
    }
    /// <summary>
    /// 方便程序寻找IMyEventHandler的实现
    /// </summary>
    public interface IMyEventHandler
    {
        Task Begin(IConnection connection);
    }
}
