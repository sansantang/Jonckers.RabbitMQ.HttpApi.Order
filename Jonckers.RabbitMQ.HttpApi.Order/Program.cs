using Jonckers.RabbitMQ.Core;
using Jonckers.RabbitMQ.HttpApi.Order;
using Jonckers.RabbitMQ.HttpApi.Order.Consumer;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// 添加MyRabbitMQ到services
builder.Services.AddMyRabbitMQ(builder.Configuration);
//builder.Services.AddMyRabbitMQEventHandlers(typeof(PerryTestEventHandler).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// [+新增] MyEventHandler
app.UseMyEventHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
