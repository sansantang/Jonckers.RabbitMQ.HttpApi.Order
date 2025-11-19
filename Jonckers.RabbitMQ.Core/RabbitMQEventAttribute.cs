using Jonckers.RabbitMQ.Core.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jonckers.RabbitMQ.Core
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

        /// <summary>
        /// 构造函数（队列名为必填参数，其他通过属性/命名参数配置）
        /// </summary>
        /// <param name="queue">队列名称（必填）</param>
        public RabbitMQEventAttribute(string queue)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            Exchange = ConfigurationManager.DefaultExchangeName; // 使用配置管理器中的默认交换机名称
        }

        public RabbitMQEventAttribute(string routingkey, string queue)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            RoutingKey = routingkey;
            Exchange = ConfigurationManager.DefaultExchangeName; // 使用配置管理器中的默认交换机名称
        }

        public RabbitMQEventAttribute(string routingkey, string queue, string exchange)
        {
            Queue = queue; // 强制必填，确保队列名不会为空
            RoutingKey = routingkey;
            Exchange = exchange; // 使用显式指定的交换机名称
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
