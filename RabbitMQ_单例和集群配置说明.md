# RabbitMQ 单例连接和集群配置修改说明

## ✅ 修改内容总结

### 1. 将 IConnection 注册为单例
- **位置**: `MyRabbiteMQExtensions.cs` 的 `AddMyRabbitMQ` 方法
- **修改**: 添加了 `IConnection` 的单例注册
- **效果**: 整个应用程序生命周期中只创建一个 RabbitMQ 连接，所有消费者共享该连接

```csharp
// 将 IConnection 注册为单例，确保整个应用只有一个 RabbitMQ 连接
services.AddSingleton<IConnection>(provider =>
{
    var factory = provider.GetRequiredService<ConnectionFactory>();
    var connection = factory.CreateConnection();
    Console.WriteLine("✅ RabbitMQ IConnection（长连接）已创建并注册为单例");
    return connection;
});
```

### 2. 修改 UseMyEventHandler 方法
- **位置**: `MyRabbiteMQExtensions.cs` 的 `UseMyEventHandler` 方法
- **修改**: 从 DI 容器获取单例连接，而不是动态创建
- **效果**: 避免重复创建连接，使用已注册的单例连接

```csharp
// ✅ 从 DI 容器获取单例的 IConnection（而不是自己创建）
var connection = app.ApplicationServices.GetService<IConnection>();
if (connection == null)
{
    throw new InvalidOperationException("RabbitMQ IConnection 未注册为单例，请检查 DI 配置");
}

Console.WriteLine($"🟢 使用已注册的 RabbitMQ 单例连接: {connection.Endpoint}");
```

### 3. 支持 RabbitMQ 集群配置
- **位置**: `MyRabbiteMQExtensions.cs` 的 `AddMyRabbitMQ` 方法
- **修改**: 支持从配置文件读取多个 RabbitMQ 节点地址
- **效果**: 支持高可用集群部署，自动故障转移

```csharp
// ✅ 支持 RabbitMQ 集群：如果配置了多个节点地址，使用 Hostnames
var hostsSection = configuration.GetSection("RabbitMQConnection:Hosts");
if (hostsSection.Exists())
{
    var hostList = new List<RabbitMQ.Client.Address>();
    var hosts = hostsSection.Get<string[]>();
    
    if (hosts != null && hosts.Length > 0)
    {
        foreach (var host in hosts)
        {
            // 支持格式："hostname:port" 或 "hostname"
            var parts = host.Split(':');
            var hostname = parts[0];
            var port = parts.Length > 1 && int.TryParse(parts[1], out int portNum) ? portNum : 5672;
            
            hostList.Add(new RabbitMQ.Client.Address(hostname, port));
            Console.WriteLine($"🐇 添加 RabbitMQ 集群节点: {hostname}:{port}");
        }
        
        connectionFactory.Hostnames = hostList;
        Console.WriteLine($"✅ 已配置 RabbitMQ 集群，共 {hostList.Count} 个节点");
    }
}
```

## 📋 配置文件示例

### 单节点配置（appsettings.json）
```json
{
  "RabbitMQConnection": {
    "UserName": "guest",
    "Password": "guest",
    "HostName": "localhost",
    "Port": 5672,
    "ExchangeName": "my.exchange"
  }
}
```

### 集群配置（appsettings.Cluster.json）
```json
{
  "RabbitMQConnection": {
    "UserName": "guest",
    "Password": "guest",
    "HostName": "localhost",
    "Port": 5672,
    "ExchangeName": "my.exchange",
    "Hosts": [
      "rabbit1.example.com:5672",
      "rabbit2.example.com:5672", 
      "rabbit3.example.com:5672"
    ]
  }
}
```

## 🎯 修改后的优势

### 1. 连接管理优化
- ✅ **单例连接**: 整个应用只创建一个 RabbitMQ 连接，避免资源浪费
- ✅ **自动恢复**: 启用了 `AutomaticRecoveryEnabled = true`，网络闪断时自动重连
- ✅ **心跳检测**: 设置了 `RequestedHeartbeat = 60`，保持连接活跃

### 2. 集群支持
- ✅ **高可用**: 支持多个 RabbitMQ 节点，自动故障转移
- ✅ **灵活配置**: 可以通过配置文件轻松切换单节点/集群模式
- ✅ **负载均衡**: 客户端会自动尝试连接可用的节点

### 3. 事件处理器优化
- ✅ **单例处理器**: 事件处理器已注册为 Singleton，确保长期运行
- ✅ **统一管理**: 所有消费者共享同一个连接，便于管理和监控

## 🔧 使用方法

### 1. 在 Program.cs 中配置
```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加 RabbitMQ 服务（自动支持单例和集群）
builder.Services.AddMyRabbitMQ(builder.Configuration);

// 添加事件处理器（自动注册为单例）
builder.Services.AddMyRabbitMQEventHandlers(typeof(Program).Assembly);

var app = builder.Build();

// 启动 RabbitMQ 事件处理器（使用单例连接）
app.UseMyEventHandler();

app.Run();
```

### 2. 配置文件选择
- **开发环境**: 使用 `appsettings.json`（单节点）
- **生产环境**: 使用 `appsettings.Production.json` 或 `appsettings.Cluster.json`（集群）

## 📊 监控和日志

修改后的代码会输出详细的日志信息：
- ✅ 连接创建状态
- 🐇 集群节点添加情况
- 🟢 使用的连接信息
- ✅ 事件处理器启动状态
- 🎉 初始化完成状态

## 🚀 性能提升

1. **资源使用**: 减少连接数量，降低内存和网络开销
2. **启动速度**: 避免重复创建连接，加快应用启动
3. **稳定性**: 单例连接 + 自动恢复，提高系统稳定性
4. **可扩展性**: 支持集群部署，便于水平扩展

## ⚠️ 注意事项

1. **连接池**: RabbitMQ.Client 本身支持通道复用，单例连接是最佳实践
2. **线程安全**: RabbitMQ.Client 的连接是线程安全的，可以被多个消费者共享
3. **配置优先级**: 如果配置了 `Hosts`，将忽略 `HostName` 和 `Port`
4. **故障转移**: 集群模式下，客户端会按顺序尝试连接各个节点