using Jonckers.RabbitMQClient.Core.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jonckers.RabbitMQClient.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RabbitMQEventAttribute : Attribute
    {
        /// <summary>
        /// 交换机名称（可选，默认从配置管理器获取）
        /// </summary>
        public string Exchange { get; }

        /// <summary>
        /// 队列名称（必填）
        /// </summary>
        public string Queue { get; }

        /// <summary>
        /// 路由键（可选，默认：队列名.route）
        /// </summary>
        public string RoutingKey { get; }

        public DeadLetterExpiration DeadLetterExpiration { get; set; } = new DeadLetterExpiration(false, 600000);

        /// <summary>
        /// 构造函数（队列名为必填参数，其他通过属性/命名参数配置）
        /// </summary>
        /// <param name="queue">队列名称（必填）</param>
        /// <param name="isWithDeadLetter">是否启用死信队列
        /// <list type="">deadLetterExchangeName = _exchangeName + ".dlx-exchange";</list>
        /// <list type="">deadLetterQueueName = _queueName + ".dlx-queue";</list>
        /// <list type="">deadLetterRoutingKey = _routingKeyName + ".dlrk-routingKey"</list>
        /// </param>
        /// <param name="expirationMilliseconds">单位：ms，多少时间过期到死信队列,必须 isWithDeadLetter = ture</param>
        public RabbitMQEventAttribute(string queue, bool isWithDeadLetter = false, int expirationMilliseconds = 600000)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            Exchange = ConfigurationManager.DefaultExchangeName; // 使用配置管理器中的默认交换机名称
            DeadLetterExpiration = new DeadLetterExpiration(isWithDeadLetter, expirationMilliseconds);
        }

        /// <summary>
        /// 构造函数（队列名和路由键为必填参数，其他通过属性/命名参数配置）
        /// </summary>
        /// <param name="routingkey"></param>
        /// <param name="queue"></param>
        /// <param name="isWithDeadLetter">是否启用死信队列
        /// <list type="">deadLetterExchangeName = _exchangeName + ".dlx-exchange";</list>
        /// <list type="">deadLetterQueueName = _queueName + ".dlx-queue";</list>
        /// <list type="">deadLetterRoutingKey = _routingKeyName + ".dlrk-routingKey"</list>
        /// </param>
        /// <param name="expirationMilliseconds">单位：ms，多少时间过期到死信队列,必须 isWithDeadLetter = ture</param>
        public RabbitMQEventAttribute(string routingkey, string queue, bool isWithDeadLetter = false, int expirationMilliseconds = 600000)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            RoutingKey = routingkey;
            Exchange = ConfigurationManager.DefaultExchangeName; // 使用配置管理器中的默认交换机名称
            DeadLetterExpiration = new DeadLetterExpiration(isWithDeadLetter, expirationMilliseconds);
        }

        /// <summary>
        /// 构造函数（队列名和路由键为必填参数，其他通过属性/命名参数配置）
        /// </summary>
        /// <param name="routingkey"></param>
        /// <param name="queue"></param>
        /// <param name="exchange"></param>
        /// <param name="isWithDeadLetter">是否启用死信队列，  
        /// <list type="">deadLetterExchangeName = _exchangeName + ".dlx-exchange";</list>
        /// <list type="">deadLetterQueueName = _queueName + ".dlx-queue";</list>
        /// <list type="">deadLetterRoutingKey = _routingKeyName + ".dlrk-routingKey"</list>
        /// </param>
        /// <param name="expirationMilliseconds">单位：ms，多少时间过期到死信队列,必须 isWithDeadLetter = ture</param>
        public RabbitMQEventAttribute(string routingkey, string queue, string exchange, bool isWithDeadLetter = false, int expirationMilliseconds = 600000)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            RoutingKey = routingkey;
            Exchange = exchange; // 使用显式指定的交换机名称
            DeadLetterExpiration = new DeadLetterExpiration(isWithDeadLetter, expirationMilliseconds);
        }
    }

    public struct DeadLetterExpiration
    {
        // 是否死信（你的 bool 标识）
        public bool IsDeadLetter { get; }
        // 对应的过期时间（毫秒）
        public int ExpirationMilliseconds { get; }

        // 构造函数强制初始化，避免默认值混乱
        public DeadLetterExpiration(bool isDeadLetter, int expirationMilliseconds)
        {
            IsDeadLetter = isDeadLetter;
            ExpirationMilliseconds = expirationMilliseconds;
        }
    }

    /// <summary>
    /// 静态配置管理器，用于非依赖注入场景下访问配置
    /// </summary>
    public static class ConfigurationManager
    {
        private static string _defaultExchangeName;

        /// <summary>
        /// 默认交换机名称
        /// </summary>
        public static string DefaultExchangeName => _defaultExchangeName ?? string.Empty;

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        /// <param name="exchangeName">默认交换机名称</param>
        public static void Initialize(string exchangeName)
        {
            _defaultExchangeName = exchangeName;
        }
    }
}
