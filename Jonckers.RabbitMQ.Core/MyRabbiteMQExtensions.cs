using Jonckers.RabbitMQ.Core.IService;
using Jonckers.RabbitMQ.Core.Options;
using Jonckers.RabbitMQ.Core.Service;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace Jonckers.RabbitMQ.Core
{
    public static class MyRabbiteMQExtensions
    {
        /// <summary>
        /// 初始化消息队列，并添加Publisher到IoC容器
        /// </summary>
        /// <remarks>从Configuration读取"MyRabbbitMQOptions配置项"</remarks>
        public static IServiceCollection AddMyRabbitMQ(this IServiceCollection services, IConfiguration configuration)
        {
            #region 配置项
            // 从Configuration读取"MyRabbbitMQOptions配置项
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
            //services.Configure<RabbitMQOptions>(optionSection);
            services.Configure<RabbitMQOptions>(options => {
                options.UserName = myOptions.UserName;
                options.Password = myOptions.Password;
                options.Host = myOptions.Host;
                options.Port = myOptions.Port;
                options.ExchangeName = myOptions.ExchangeName;
            });
            #endregion

            // 加了这行，才可以注入任意类型参数的 IMyPublisher<> 使用
            services.AddTransient(typeof(IMyPublisher<>), typeof(MyPublisher<>));

            // 创建一个工厂对象，并配置单例注入
            services.AddSingleton(new ConnectionFactory
            {
                UserName = myOptions.UserName,
                Password = myOptions.Password,
                HostName = myOptions.Host,
                Port = myOptions.Port
            });

            return services;
        }

        /// <summary>
        /// IServiceCollection的拓展方法，用于发现自定义的EventHandler并添加到服务容器
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
                        // 瞬态注入配置
                        services.AddTransient(typeof(IMyEventHandler), type);
                        Console.WriteLine($"已注册事件处理器: {type.FullName}");
                    }
                }
            }

            return services;
        }

        /// <summary>
        /// IServiceCollection的拓展方法，用于发现自定义的EventHandler并添加到服务容器
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
                    // 瞬态注入配置
                    services.AddTransient(typeof(IMyEventHandler), type);
                    Console.WriteLine($"已注册事件处理器: {type.FullName}");
                }
            }

            return services;
        }

       /// <summary>
        /// 给app拓展方法
        /// </summary>
        /// <remarks>
        /// 在IoC容器里获取到所有继承自IMyEventHandler的实现类，并开启消费者
        /// </remarks>
        public static async Task<IApplicationBuilder> UseMyEventHandler(this IApplicationBuilder app)
        {
            // 将事件处理器存储在静态变量中，防止被垃圾回收
            var eventHandlerHolder = new List<IMyEventHandler>();
            
            try
            {
                var handlers = app.ApplicationServices.GetServices(typeof(IMyEventHandler));
                var factory = app.ApplicationServices.GetService<ConnectionFactory>();
                                
                if (!handlers.Any())
                {
                    Console.WriteLine("未发现任何事件处理器");
                    return app;
                }
                
                // 获取连接（同步方式，避免异步上下文问题）
                var connection = await factory.CreateConnectionAsync();
                Console.WriteLine("RabbitMQ connection established");

                // 遍历调用自定义的Begin方法
                foreach (var h in handlers)
                {
                    var handler = h as IMyEventHandler;
                    if (handler != null)
                    {
                        handler.Begin(connection).Wait(); // 同步等待确保初始化完成
                        eventHandlerHolder.Add(handler); // 保存引用防止被垃圾回收
                        Console.WriteLine($"Handler {h.GetType().Name} started successfully");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to cast handler: {h.GetType().Name}");
                    }
                }
                
                // 将事件处理器持有者注册为单例，确保不会被垃圾回收
                app.ApplicationServices.GetService<System.IServiceProvider>()
                    .GetService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();
                
                Console.WriteLine("所有事件处理器初始化完成");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing event handlers: {ex.Message}, InnerException: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return app;
        }
    }
}
