using Jonckers.RabbitMQ.Core.IService;
using Jonckers.RabbitMQ.Core.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Core.Service
{
    public class MyPublisher<T> : IMyPublisher<T>, IDisposable where T : class
    {
        private readonly RabbitMQOptions _myOptions;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly string _queueName;
        private bool _disposed = false;
        /// <summary>
        /// 非注入时使用此构造方法
        /// </summary>
        /// <param name="connection">RabbitMQ连接</param>
        /// <exception cref="ArgumentNullException">当连接为空时抛出</exception>
        public MyPublisher(IConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));

            // 初始化队列名称
            var type = typeof(T);
            var attr = type.GetCustomAttribute<RabbitMQEventAttribute>();
            _queueName = string.IsNullOrWhiteSpace(attr?.Queue) ? type.FullName : attr.Queue;

            // 在同步构造函数中使用异步方法的同步调用
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 创建发布者的异步工厂方法
        /// </summary>
        /// <param name="optionsMonitor">配置监控器</param>
        /// <param name="factory">连接工厂</param>
        /// <returns>异步任务，包含创建的发布者实例</returns>
        public static async Task<MyPublisher<T>> CreateAsync(IOptionsMonitor<RabbitMQOptions> optionsMonitor, ConnectionFactory factory)
        {
            if (optionsMonitor == null) throw new ArgumentNullException(nameof(optionsMonitor));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var options = optionsMonitor.CurrentValue;

            // 异步创建连接和通道
            var connection = await factory.CreateConnectionAsync().ConfigureAwait(false);
            var channel = await connection.CreateChannelAsync().ConfigureAwait(false);

            // 异步声明交换机
            await channel.ExchangeDeclareAsync(
                options.ExchangeName,
                ExchangeType.Direct,
                durable: false,
                autoDelete: false,
                arguments: null).ConfigureAwait(false);

            // 获取队列名称
            var type = typeof(T);
            var attr = type.GetCustomAttribute<RabbitMQEventAttribute>();
            var queueName = string.IsNullOrWhiteSpace(attr?.Queue) ? type.FullName : attr.Queue;

            // 异步声明队列
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null).ConfigureAwait(false);

            // 异步绑定队列到交换机
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: options.ExchangeName,
                routingKey: queueName,
                arguments: null).ConfigureAwait(false);

            // 使用私有构造函数创建实例
            return new MyPublisher<T>(options, connection, channel, queueName);
        }

        /// <summary>
        /// 私有构造函数，供工厂方法使用
        /// </summary>
        private MyPublisher(RabbitMQOptions options, IConnection connection, IChannel channel, string queueName)
        {
            _myOptions = options;
            _connection = connection;
            _channel = channel;
            _queueName = queueName;
        }

        /// <summary>
        /// 依赖注入使用的构造方法
        /// </summary>
        /// <param name="optionsMonitor">配置监控器</param>
        /// <param name="factory">连接工厂</param>
        /// <remarks>
        /// 注意：此构造函数在初始化时会同步阻塞等待异步操作完成，可能在高并发环境下导致性能问题。
        /// 推荐使用CreateAsync工厂方法进行异步初始化。
        /// </remarks>
        public MyPublisher(IOptionsMonitor<RabbitMQOptions> optionsMonitor, ConnectionFactory factory)
        {
            if (optionsMonitor == null) throw new ArgumentNullException(nameof(optionsMonitor));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _myOptions = optionsMonitor.CurrentValue;

            try
            {
                // 注意：在构造函数中使用.GetAwaiter().GetResult()比.Result更安全，可以避免某些死锁情况
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

                // 声明Exchange
                _channel.ExchangeDeclareAsync(
                    _myOptions.ExchangeName,
                    ExchangeType.Direct,
                    false,
                    false,
                    null).GetAwaiter().GetResult();

                var type = typeof(T);
                // 获取类上的QueueNameAttribute特性，如果不存在则使用类的完整名
                var attr = type.GetCustomAttribute<RabbitMQEventAttribute>();
                _queueName = string.IsNullOrWhiteSpace(attr?.Queue) ? type.FullName : attr.Queue;

                // 声明队列
                _channel.QueueDeclareAsync(
                    _queueName,
                    true,
                    false,
                    false,
                    null).GetAwaiter().GetResult();

                // 将队列绑定到交换机
                _channel.QueueBindAsync(
                    _queueName,
                    _myOptions.ExchangeName,
                    _queueName,
                    null).GetAwaiter().GetResult();
            }
            catch
            {
                // 发生异常时，确保资源被释放
                _channel?.Dispose();
                _connection?.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="data">要发布的消息数据</param>
        /// <param name="encoding">字符编码，默认为UTF-8</param>
        /// <returns>异步任务</returns>
        /// <exception cref="ArgumentNullException">当数据为空时抛出</exception>
        /// <exception cref="ObjectDisposedException">当发布者已被释放时抛出</exception>
        public async Task PublishAsync(T data, Encoding encoding = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MyPublisher<T>));
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                // 对象序列化为JSON
                var msg = JsonConvert.SerializeObject(data);
                byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(msg);


                var properties = new BasicProperties
                {
                    Persistent = true
                };


                // 使用异步方法发布消息
                await _channel.BasicPublishAsync(
                    exchange: _myOptions.ExchangeName,
                    routingKey: _queueName,
                    body: bytes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 可以根据需要记录日志或进行其他错误处理
                throw new InvalidOperationException($"发布消息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routingKey"></param>
        /// <param name="data">消息</param>
        /// <param name="exchangeName">交换机名称</param>
        /// <param name="expiration">消息超时时间，例如"60000"[60秒]</param>
        /// <param name="encoding">Encoding 编码</param>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task PublishAsync(string routingKey, T data, string exchangeName = "", string expiration = "", Encoding encoding = null)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(MyPublisher<T>));
            if (data == null) throw new ArgumentNullException(nameof(data));

            try
            {
                if (exchangeName != string.Empty)
                {
                    //await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct,durable: true,autoDelete: false, null);
                    await _channel.ExchangeDeclareAsync(exchangeName, ExchangeType.Direct);

                    await _channel.QueueBindAsync(
                        queue: _queueName,
                        exchange: exchangeName,
                        routingKey: routingKey
                    );
                }


                // 对象序列化为JSON
                var msg = JsonConvert.SerializeObject(data);
                byte[] bytes = (encoding ?? Encoding.UTF8).GetBytes(msg);

                var properties = new BasicProperties
                {
                    Persistent = true
                };

                if (!string.IsNullOrWhiteSpace(expiration) && int.TryParse(expiration, out _))
                {
                    properties.Expiration = expiration;
                }

                // 使用异步方法发布消息
                await _channel.BasicPublishAsync(
                    exchange: string.IsNullOrEmpty(exchangeName) ? _myOptions.ExchangeName : exchangeName,
                    routingKey: routingKey, 
                    mandatory: false,
                    basicProperties: properties,
                    body: bytes).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 可以根据需要记录日志或进行其他错误处理
                throw new InvalidOperationException($"发布消息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的实际实现
        /// </summary>
        /// <param name="disposing">是否从Dispose方法调用</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    _channel?.Dispose();
                    _connection?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数，确保资源被释放
        /// </summary>
        ~MyPublisher()
        {
            Dispose(false);
        }
    }
}
