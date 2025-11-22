using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Core.Extend
{
    public static class DieLetterExchangeExtend
    {

        /// <summary>
        /// 扩展方法：初始化死信交换机+死信队列+绑定关系
        /// </summary>
        /// <param name="channel">扩展的 IChannel 实例（this 关键字标记）</param>
        /// <param name="dlxExchangeName">死信交换机名称（动态传入，替代硬编码）</param>
        /// <param name="dlqQueueName">死信队列名称（动态传入，增强通用性）</param>
        /// <param name="routingKey">绑定路由键（动态传入，灵活配置）</param>
        /// <returns></returns>
        public static async Task InitDieLetterExchangeAsync(
            this IChannel channel,  // this 关键字是扩展方法的核心标记，不能少
            string dlxExchangeName,
            string dlqQueueName,
            string routingKey)
        {
            // 校验参数（避免空值导致 RabbitMQ 操作失败）
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            if (string.IsNullOrWhiteSpace(dlxExchangeName)) throw new ArgumentException("死信交换机名称不能为空", nameof(dlxExchangeName));
            if (string.IsNullOrWhiteSpace(dlqQueueName)) throw new ArgumentException("死信队列名称不能为空", nameof(dlqQueueName));
            if (string.IsNullOrWhiteSpace(routingKey)) throw new ArgumentException("路由键不能为空", nameof(routingKey));

            // 1. 声明死信交换机（使用传入的 exchangeName，而非硬编码）
            await channel.ExchangeDeclareAsync(
                exchange: dlxExchangeName,
                type: ExchangeType.Direct,
                durable: true,  // 持久化：重启 RabbitMQ 后不丢失
                autoDelete: false);  // 不自动删除：连接断开后不删除

            // 2. 声明死信队列（使用传入的队列名，而非硬编码）
            await channel.QueueDeclareAsync(
                queue: dlqQueueName,
                durable: true,  // 持久化队列
                exclusive: false,  // 非独占：多个连接可访问
                autoDelete: false);  // 不自动删除

            // 3. 绑定死信队列到死信交换机（使用传入的路由键）
            await channel.QueueBindAsync(
                queue: dlqQueueName,
                exchange: dlxExchangeName,
                routingKey: routingKey);
        }
    }
}
