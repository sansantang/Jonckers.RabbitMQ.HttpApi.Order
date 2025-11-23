# RabbitMQ 优化说明

## 🎯 优化目标

1. **将 RabbitMQ 连接（IConnection）注册为单例**，避免重复创建连接
2. **将事件处理器（IMyEventHandler）注册为单例**，确保消费者长期运行
3. **支持集群配置**，提高可用性和故障转移能力
4. **改进日志输出**，便于调试和监控

## 📁 文件说明

### 1. `MyRabbiteMQExtensions_Optimized.cs`
- 优化后的基础版本
- 将 `IConnection` 注册为单例
- 将 `IMyEventHandler` 注册为单例
- 改进了 `UseMyEventHandler` 方法，使用单例连接

### 2. `MyRabbiteMQExtensions_Cluster.cs`
- 支持集群配置的增强版本
- 支持多节点配置和自动故障转移
- 包含所有优化功能

### 3. `appsettings.ClusterExample.json`
- 集群配置示例文件
- 展示如何配置多个 RabbitMQ 节点

## 🚀 使用方法

### 方法一：使用优化版本（单节点）

```csharp
// 在 Program.cs 中
var builder = WebApplication.CreateBuilder(args);

// 使用优化版本
builder.Services.AddMyRabbitMQ(builder.Configuration);

// 注册事件处理器
builder.Services.AddMyRabbitMQEventHandlers(typeof(DeadLetterTestEventHandler).Assembly);

var app = builder.Build();

// 启动事件处理器
app.UseMyEventHandler();

app.Run();
```

### 方法二：使用集群版本（推荐）

```csharp
// 在 Program.cs 中
var builder = WebApplication.CreateBuilder(args);

// 使用集群版本
builder.Services.AddMyRabbitMQWithCluster(builder.Configuration);

// 注册事件处理器
builder.Services.AddMyRabbitMQEventHandlers(typeof(DeadLetterTestEventHandler).Assembly);

var app = builder.Build();

// 启动事件处理器
app.UseMyEventHandler();

app.Run();
```

## ⚙️ 配置说明

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

### 集群配置（appsettings.json）
```json
{
  "RabbitMQConnection": {
    "UserName": "guest",
    "Password": "guest",
    "HostName": "localhost",  // 备用单节点地址
    "Port": 5672,
    "ExchangeName": "my.exchange",
    "VirtualHost": "/",
    "ClusterNodes": [
      "rabbit1.example.com:5672",
      "rabbit2.example.com:5672",
      "rabbit3.example.com:5672"
    ]
  }
}
```

## ✅ 优化效果对比

| 项目 | 优化前 | 优化后 |
|------|--------|--------|
| **ConnectionFactory** | 单例 ✅ | 单例 ✅ |
| **IConnection** | 每次动态创建 ❌ | 单例 ✅ |
| **IMyEventHandler** | 瞬态（Transient）❌ | 单例 ✅ |
| **集群支持** | 不支持 ❌ | 支持 ✅ |
| **连接恢复** | 基础 | 增强配置 ✅ |
| **日志输出** | 基础 | 详细友好 ✅ |

## 🔧 主要改进点

### 1. 连接管理优化
```csharp
// 优化前：每次都创建新连接
connection = await factory.CreateConnectionAsync();

// 优化后：使用单例连接
var connection = app.ApplicationServices.GetService<IConnection>();
```

### 2. 事件处理器生命周期优化
```csharp
// 优化前：瞬态注入
services.AddTransient(typeof(IMyEventHandler), type);

// 优化后：单例注入
services.AddSingleton(typeof(IMyEventHandler), type);
```

### 3. 集群支持
```csharp
// 支持多节点配置
factory.Hostnames = new List<Address>
{
    new Address("rabbit1", 5672),
    new Address("rabbit2", 5672),
    new Address("rabbit3", 5672)
};

// 启用自动恢复
factory.AutomaticRecoveryEnabled = true;
factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);
```

## 🎉 使用建议

1. **生产环境推荐使用集群版本** (`MyRabbiteMQExtensions_Cluster.cs`)
2. **开发环境可以使用优化版本** (`MyRabbiteMQExtensions_Optimized.cs`)
3. **确保配置文件正确**，特别是集群节点的地址和端口
4. **监控日志输出**，确保所有事件处理器都成功启动

## 🐛 故障排查

### 常见问题：

1. **连接失败**
   - 检查用户名、密码是否正确
   - 检查 RabbitMQ 服务是否运行
   - 检查防火墙设置

2. **事件处理器启动失败**
   - 检查队列是否存在
   - 检查权限配置
   - 查看详细错误日志

3. **集群连接问题**
   - 确保所有节点都在运行
   - 检查节点间网络连通性
   - 验证集群配置是否正确

## 📞 技术支持

如有问题，请检查：
1. 控制台日志输出
2. RabbitMQ 管理界面
3. 网络连接状态
4. 配置文件格式