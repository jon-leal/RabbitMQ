using System.Text.Json;
using RabbitMQ.Core.Configuration;
using RabbitMQ.Core.Interfaces;
using RabbitMQ.Core.Models;

namespace RabbitMQ.Core;

public class SendMessageService
{
    private readonly IRabbitMQService _rabbitMQService;
    private readonly RabbitMQConfig _config;

    public SendMessageService(IRabbitMQService rabbitMQService, RabbitMQConfig config)
    {
        _rabbitMQService = rabbitMQService;
        _config = config;
    }

    public void SendMessage()
    {
        var messageRequest = new MessageModel
        {
            Id = Guid.NewGuid().ToString(),
            Content = "Hello World!",
            CreatedAt = DateTime.UtcNow
        };

        // Abordagem 1: Estou usando atualmente serializar e enviar como string
        var message = JsonSerializer.Serialize(messageRequest);
        _rabbitMQService.Publish(_config.QueueName, message);
        
        // Abordagem 2: método genérico (é necessário alterar a interface e implementação)
        // _rabbitMQService.Publish<MessageModel>(_config.QueueName, messageRequest);

        // Exemplo com DLQ (Dead Letter Queue)
        // _rabbitMQService.Publish( exchange: _config.ExchangeName, deadLetterExchange: _config.DeadLetterExchange, queue: _config.QueueName, routingKey: _config.RoutingKey, deadLetterRoutingKey: _config.DeadLetterRoutingKey, message: message);
    }
}