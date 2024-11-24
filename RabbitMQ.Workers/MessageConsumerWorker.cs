using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Core.Configuration;
using RabbitMQ.Core.Models;

namespace RabbitMQ.Workers;

public class MessageConsumerWorker : BackgroundService
{
    private readonly ILogger<MessageConsumerWorker> _logger;
    private readonly RabbitMQConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public MessageConsumerWorker(ILogger<MessageConsumerWorker> logger, RabbitMQConfig config, IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _config = config;
        _serviceScopeFactory = serviceScopeFactory;
        _connection = InitializeRabbitMQConnection();
        _channel = _connection.CreateModel();
    }

    private IConnection InitializeRabbitMQConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.HostName,
                UserName = _config.UserName,
                Password = _config.Password,
                Port = _config.Port,
                VirtualHost = _config.VirtualHost,
            };

            return factory.CreateConnection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inicializar conexão com RabbitMQ");
            throw;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Iniciando consumer...");

            SetupQueuesAndExchanges();
            ConfigureConsumer();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro na execução do consumer");
            throw;
        }
    }

    private void SetupQueuesAndExchanges()
    {
        try
        {
            _channel.ExchangeDeclare(exchange: _config.ExchangeName, type: "direct", durable: true, autoDelete: false);
            _channel.ExchangeDeclare(exchange: _config.DeadLetterExchange, type: "direct", durable: true, autoDelete: false);
            
            var queueArgs = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", _config.DeadLetterExchange },
                { "x-dead-letter-routing-key", _config.DeadLetterRoutingKey },
            };
           
            _channel.QueueDeclare(queue: _config.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
            _channel.QueueBind(queue: _config.QueueName, exchange: _config.ExchangeName, routingKey: _config.RoutingKey);
            _channel.QueueDeclare(queue: _config.DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: _config.DeadLetterQueue, exchange: _config.DeadLetterExchange, routingKey: _config.DeadLetterRoutingKey);
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao configurar filas e exchanges");
            throw;
        }
    }

    private void ConfigureConsumer()
    {
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) => await ProcessMessageAsync(ea);

        _channel.BasicConsume(queue: _config.QueueName, autoAck: false, consumer: consumer);
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs ea)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);

        try
        {
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation($"Mensagem recebida: {message}");

            var messageModel = JsonSerializer.Deserialize<MessageModel>(message);

            using var scope = _serviceScopeFactory.CreateScope();
            await ProcessMessage(messageModel);
            
            _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogInformation($"Mensagem processada com sucesso: {messageModel.Id}");
        }
        catch (Exception ex)
        {
            await HandleProcessingError(ex, ea, retryCount);
        }
    }

    private int GetRetryCount(IBasicProperties properties)
    {
        if (properties?.Headers != null && 
            properties.Headers.TryGetValue("x-retry", out var value))
        {
            return (int)value;
        }
        return 0;
    }

    private async Task HandleProcessingError(Exception ex, BasicDeliverEventArgs ea, int retryCount)
    {
        _logger.LogError(ex, "Erro ao processar mensagem");

        if (retryCount < 3)
        {
            // Reencaminha para a mesma fila com contagem de retry incrementada
            var properties = _channel.CreateBasicProperties();
            properties.Headers = new Dictionary<string, object>
            {
                { "x-retry", retryCount + 1 }
            };

            _channel.BasicPublish(exchange: _config.ExchangeName, routingKey: _config.RoutingKey, basicProperties: properties, body: ea.Body);
            _channel.BasicAck(ea.DeliveryTag, false);
            _logger.LogWarning($"Mensagem reenfileirada. Tentativa {retryCount + 1} de 3");
        }
        else
        {
            // Rejeita a mensagem após 3 tentativas, enviando para DLQ
            _channel.BasicNack(ea.DeliveryTag, false, false);
            _logger.LogError("Mensagem rejeitada e enviada para DLQ após 3 tentativas");
        }
    }

    private async Task ProcessMessage(MessageModel message)
    {
        // Implementar aqui o processamento da mensagem (apontar services, repository etc)
        await Task.Delay(100);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Parando consumer...");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        _connection?.Close();
        _connection?.Dispose();
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}