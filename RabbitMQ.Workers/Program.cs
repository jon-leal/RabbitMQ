using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Core;
using RabbitMQ.Core.Configuration;
using RabbitMQ.Core.Interfaces;
using RabbitMQ.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configuração do RabbitMQ
builder.Services.Configure<RabbitMQConfig>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<RabbitMQConfig>>().Value);

builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHostedService<MessageConsumerWorker>();

builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.AddDebug();
});

var host = builder.Build();
host.Run();