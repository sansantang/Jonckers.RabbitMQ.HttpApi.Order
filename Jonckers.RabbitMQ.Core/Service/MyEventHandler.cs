using Jonckers.RabbitMQClient.Core.IService;
using Jonckers.RabbitMQClient.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQClient.Core.Service
{
    public abstract class MyEventHandler<T> : IMyEventHandler<T> where T : class
    {
        private IChannel _channel;
        private string _routingKey;
        private string _queueName;
        private string _exchangeName;
        private AsyncEventingBasicConsumer _consumer;
        // 添加一个字段来保持对消费者的引用，防止被垃圾回收
        private static readonly List<object> _strongReferences = new List<object>();
        public MyEventHandlerOptions Options = new MyEventHandlerOptions()
        {
            DisableDeserializeObject = false
        };

        public async Task Begin(IConnection connection)
        {
            var type = typeof(T);
            Console.WriteLine($"开始初始化事件处理器: {type.Name}");

            // 获取类上的QueueNameAttribute特性，如果不存在则使用类的完整名
            var attr = type.GetCustomAttribute<RabbitMQEventAttribute>();
            _queueName = string.IsNullOrWhiteSpace(attr?.Queue) ? type.FullName : attr.Queue;
            _exchangeName = string.IsNullOrWhiteSpace(attr?.Exchange) ? "" : attr.Exchange;
            _routingKey = string.IsNullOrWhiteSpace(attr?.RoutingKey) ? "" : attr.RoutingKey;

            Console.WriteLine($"队列配置 - Queue: {_queueName}, Exchange: {_exchangeName}, RoutingKey: {_routingKey}");

            //创建通道
            _channel = await connection.CreateChannelAsync();

            // 设置 QoS - 每次只处理一条消息
            await _channel.BasicQosAsync(0, 1, false);

            Console.WriteLine("通道创建成功");

            if (Options.PrefetchSize > 0 || Options.PrefetchCount > 0)
            {
                await _channel.BasicQosAsync(
                    Options.PrefetchSize,
                    Options.PrefetchCount > 0 ? Options.PrefetchCount : (ushort)1,
                    Options.Global);
                Console.WriteLine($"QoS 设置完成 (prefetchSize={Options.PrefetchSize}, prefetchCount={Options.PrefetchCount}, global={Options.Global})");
            }
            else
            {
                Console.WriteLine("使用默认QoS设置");
            }

            if (!string.IsNullOrEmpty(_exchangeName))
            {
                await _channel.ExchangeDeclareAsync(
                        _exchangeName,
                        ExchangeType.Direct);
                Console.WriteLine("交换机声明完成");
            }

            // 异步声明队列
            //await _channel.QueueDeclareAsync(
            //    queue: _queueName,
            //    durable: true,
            //    exclusive: false,
            //    autoDelete: false,
            //    arguments: null);
            //Console.WriteLine("队列声明完成");

            //if (_routingKey != "")
            //{
            //    await _channel.QueueBindAsync(
            //    queue: _queueName,
            //    exchange: _exchangeName,
            //    routingKey: _routingKey
            //    );
            //    Console.WriteLine("队列绑定完成");
            //}



            _consumer = new AsyncEventingBasicConsumer(_channel);
            // 将消费者添加到静态列表中，防止被垃圾回收
            _strongReferences.Add(_consumer);

            Console.WriteLine($"_queueName:{_queueName},routingKey:{_routingKey},exchange:{_exchangeName}");

            // 保存方法引用
            _consumer.ReceivedAsync += MyReceivedHandler;
            _strongReferences.Add(new Action<AsyncEventingBasicConsumer, object, BasicDeliverEventArgs>((c, s, e) => MyReceivedHandler(s, e).ConfigureAwait(false)));

            //消费者
            var consumerTag = await _channel.BasicConsumeAsync(_queueName, false, _consumer);
            Console.WriteLine($"消费者已启动，监听队列: {_queueName}，消费者标签: {consumerTag}");
        }

        // 收到消息后
        private async Task MyReceivedHandler(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                // 如果未配置禁用则不解析，后面抽象方法的data参数会始终为空
                if (!Options.DisableDeserializeObject)
                {
                    T data = null;
                    // 反序列化为对象
                    var message = Options.Encoding.GetString(e.Body.Span);
                    data = JsonConvert.DeserializeObject<T>(message);
                    await OnReceivedAsync(data, message);
                    // 确认该消息已被消费
                    _channel?.BasicAckAsync(e.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"exception:{ex.Message}, innerException:{ex?.InnerException?.Message}");
                OnConsumerException(ex);
            }
        }

        /// <summary>
        /// 收到消息 
        /// </summary>
        /// <param name="data">解析后的对象</param>
        /// <param name="message">消息原文</param> 
        /// <remarks>Options.DisableDeserializeObject为true时，data始终为null</remarks>
        public abstract Task OnReceivedAsync(T data, string message);

        /// <summary>
        /// 异常
        /// </summary>
        /// <param name="ex">派生类不重写的话，异常被隐藏</param>
        public virtual void OnConsumerException(Exception ex)
        {

        }
    }
}
