using Jonckers.RabbitMQ.Core.IService;
using Jonckers.RabbitMQ.Core.Options;
using Jonckers.RabbitMQ.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Core
{
    public static class MyRabbitMQClusterExtensions
    {
        /// <summary>
        /// 初始化消息队列，并添加Publisher到IoC容器（支持集群配置）
        /// </summary>
        /// <remarks>从Configuration读取"RabbitMQConnection配置项，支持多节点集群</remarks>
        public static IServiceCollection AddMyRabbitMQWithCluster(this IServiceCollection services, IConfiguration configuration)
        {
            #region 配置项
            // 从Configuration读取"RabbitMQConnection配置项
            var optionSection = configuration.GetSection("RabbitMQConnection");

            // 这个myOptions是当前方法使用
            RabbitMQOptions myOptions = new RabbitMQOptions
            {
                UserName = optionSection["UserName"],
                Password = optionSection["Password"],
                Host = optionSection["HostName"],
                Port = int.TryParse(optionSection["Port"], out int port) ? port : 5672, // 默认RabbitMQ端口
                ExchangeName = optionSection["ExchangeName"]
            };

            // 初始化静态配置管理器，设置默认交换机名称
            ConfigurationManager.Initialize(myOptions.ExchangeName);

            // 加了这行，才可以注入IOptions<RabbitMQOptions>或者IOptionsMonitor<RabbitMQOptions>
            services.Configure<RabbitMQOptions>(options =>
            {
                options.UserName = myOptions.UserName;
                options.Password = myOptions.Password;
                options.Host = myOptions.Host;
                options.Port = myOptions.Port;
                options.ExchangeName = myOptions.ExchangeName;
            });
            #endregion

            // 加了这行，才可以注入任意类型参数的 IMyPublisher<> 使用
            services.AddTransient(typeof(IMyPublisher<>), typeof(MyPublisher<>));

            // 创建支持集群的 ConnectionFactory
            var factory = new ConnectionFactory
            {
                UserName = myOptions.UserName,
                Password = myOptions.Password,
                // 启用自动连接恢复，支持集群故障转移
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = 60,
                // 设置虚拟主机（可选）
                VirtualHost = optionSection["VirtualHost"] ?? "/"
            };

            // ✅ 支持集群配置：检查是否有多个节点配置
            var clusterNodes = optionSection.GetSection("ClusterNodes").Get<List<string>>();
            if (clusterNodes != null && clusterNodes.Any())
            {
                // 解析集群节点地址，格式：["host1:port1", "host2:port2", ...]
                var addresses = new List<Address>();
                foreach (var node in clusterNodes)
                {
                    var parts = node.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int nodePort))
                    {
                        addresses.Add(new Address(parts[0], nodePort));
                    }
                    else
                    {
                        // 如果没有指定端口，使用默认端口
                        addresses.Add(new Address(node, myOptions.Port));
                    }
                }
                
                factory.Hostnames = addresses;
                Console.WriteLine($"🌐 配置 RabbitMQ 集群节点: {string.Join(", ", addresses)}");
            }
            else
            {
                // 单节点配置
                factory.HostName = myOptions.Host;
                factory.Port = myOptions.Port;
                Console.WriteLine($"🔗 配置 RabbitMQ 单节点: {myOptions.Host}:{myOptions.Port}");
            }

            // 注册 ConnectionFactory 为单例
            services.AddSingleton(factory);

            // 将 IConnection 注册为单例，确保整个应用只有一个 RabbitMQ 连接
            services.AddSingleton<IConnection>(provider =>
            {
                var connectionFactory = provider.GetRequiredService<ConnectionFactory>();
                var connection = connectionFactory.CreateConnection();
                Console.WriteLine("✅ RabbitMQ 长连接已创建并注册为单例");
                Console.WriteLine($"🔗 连接端点: {connection.Endpoint}");
                Console.WriteLine($"🔗 连接状态: {(connection.IsOpen ? "已连接" : "未连接")}");
                return connection;
            });

            return services;
        }

        /// <summary>
        /// IServiceCollection的拓展方法，用于发现自定义的EventHandler并添加到服务容器（单例模式）
        /// </summary> 
        /// <param name="assemblies">包含了自定义Handler的程序集集合</param> 
        /// <remarks>遍历所有assemblies，将继承自IMyEventHandler的类注册到容器</remarks>
        public static IServiceCollection AddMyRabbitMQEventHandlers(this IServiceCollection services, params System.Reflection.Assembly[] assemblies)
        {
            var baseType = typeof(IMyEventHandler);

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                foreach (var type in types)
                {
                    // baseType可以放type，并且type不是baseType
                    if (baseType.IsAssignableFrom(type) && baseType != type)
                    {
                        // 单例注入配置 - 事件处理器应该是长期运行的消费者
                        services.AddSingleton(typeof(IMyEventHandler), type);
                        Console.WriteLine($"已注册事件处理器: {type.FullName}");
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// IServiceCollection的拓展方法，用于发现自定义的EventHandler并添加到服务容器（单例模式）
        /// </summary> 
        /// <param name="types">包含了自定义Handler的类集合</param> 
        /// <remarks>遍历所有types，将继承自IMyEventHandler的类注册到容器</remarks>
        public static IServiceCollection AddMyRabbitMQEventHandlers(this IServiceCollection services, params Type[] types)
        {
            var baseType = typeof(IMyEventHandler);

            foreach (var type in types)
            {
                // baseType可以放type，并且type不是baseType
                if (baseType.IsAssignableFrom(type) && baseType != type)
                {
                    // 单例注入配置 - 事件处理器应该是长期运行的消费者
                    services.AddSingleton(typeof(IMyEventHandler), type);
                    Console.WriteLine($"已注册事件处理器: {type.FullName}");
                }
            }

            return services;
        }

        /// <summary>
        /// 注册并启动所有 RabbitMQ 事件处理器（基于 IMyEventHandler 的消费者）。
        /// 该方法会在应用启动时被调用，用于初始化所有实现了 IMyEventHandler 接口的消费者，
        /// 使用已注册的单例 RabbitMQ 连接，并启动消息监听。
        /// </summary>
        /// <param name="app">ASP.NET Core 的应用程序构建器（IApplicationBuilder），通常传入 app 对象。</param>
        /// <returns>返回传入的 IApplicationBuilder，以支持链式调用。</returns>
        /// <remarks>
        /// 该方法会：
        /// 1. 从依赖注入容器中获取所有 IMyEventHandler 实现类的实例；
        /// 2. 从 DI 容器获取已注册的单例 RabbitMQ 连接（IConnection）；
        /// 3. 遍历每个事件处理器，调用其 Begin(connection) 方法以启动消费者并开始监听队列；
        /// 4. 若没有找到任何事件处理器或连接未注册，将输出日志并做适当处理；
        /// 5. 保证事件处理器不会被 GC 回收，以维持 RabbitMQ 消费者长连接。
        /// </remarks>
        public static IApplicationBuilder UseMyEventHandler(this IApplicationBuilder app)
        {
            try
            {
                // 1. 获取所有已注册的事件处理器（单例）
                var handlers = app.ApplicationServices.GetServices<IMyEventHandler>().ToList();

                if (!handlers.Any())
                {
                    Console.WriteLine("⚠️ 未发现任何事件处理器");
                    return app;
                }

                Console.WriteLine($"📋 发现 {handlers.Count} 个事件处理器");

                // 2. ✅ 从 DI 容器获取单例的 IConnection（而不是动态创建）
                var connection = app.ApplicationServices.GetService<IConnection>();
                if (connection == null)
                {
                    throw new InvalidOperationException("❌ RabbitMQ IConnection 未注册为单例，请检查 DI 配置");
                }

                Console.WriteLine($"🟢 使用已注册的 RabbitMQ 单例连接: {connection.Endpoint}");
                Console.WriteLine($"🔗 连接状态: {(connection.IsOpen ? "已连接" : "未连接")}");

                // 3. 遍历调用每个处理器的 Begin 方法启动消费者
                foreach (var handler in handlers)
                {
                    try
                    {
                        handler.Begin(connection).Wait(); // 同步等待确保初始化完成
                        Console.WriteLine($"✅ Handler {handler.GetType().Name} 启动成功");
                    }
                    catch (Exception handlerEx)
                    {
                        Console.WriteLine($"❌ Handler {handler.GetType().Name} 启动失败: {handlerEx.Message}");
                    }
                }

                Console.WriteLine("🎉 所有事件处理器初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error 初始化事件处理器: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   InnerException: {ex.InnerException.Message}");
                }
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw; // 重新抛出异常，让应用启动失败以便及时发现问题
            }

            return app;
        }
    }
}