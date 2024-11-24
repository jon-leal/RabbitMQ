using RabbitMQ.Client;
using System.Text;
using RabbitMQ.Core.Configuration;
using RabbitMQ.Core.Interfaces;

namespace RabbitMQ.Core;

public class RabbitMQService : IRabbitMQService
{
    private readonly RabbitMQConfig _config;

    public RabbitMQService(RabbitMQConfig config)
    {
        _config = config;
    }

    // Método 1: Publicação simples direto na fila
    public void Publish(string queueName, string message)
    {
        var factory = CreateConnectionFactory();

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
        
        
        channel.QueueDeclare(
            queue: queueName,
            durable: true,          // Fila persiste após restart do broker
            exclusive: false,        // Fila pode ser acessada por outras conexões
            autoDelete: false,       // Fila não é deletada quando não há consumidores
            arguments: null);       

        var body = Encoding.UTF8.GetBytes(message);
        
        channel.BasicPublish(
            exchange: "",           // default exchange
            routingKey: queueName, 
            basicProperties: null,
            body: body);
    }

    // Método 2: Publicação completa com suporte a DLQ
    public void Publish(string exchange, string deadLetterExchange, string queue, string routingKey, string deadLetterRoutingKey, string message, IBasicProperties properties = null)
    {
        var factory = CreateConnectionFactory();
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();
       
        var args = new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", deadLetterExchange },
            { "x-dead-letter-routing-key", deadLetterRoutingKey }
        };
        
        channel.ExchangeDeclare(exchange, "direct", true);
        channel.ExchangeDeclare(deadLetterExchange, "direct", true);
        channel.QueueDeclare(queue: queue, durable: true, exclusive: false, autoDelete: false, arguments: args);
        channel.QueueBind(queue: queue, exchange: exchange, routingKey: routingKey);
        
        var body = Encoding.UTF8.GetBytes(message);
        
        channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: properties, body: body);
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _config.HostName,
            UserName = _config.UserName,
            Password = _config.Password,
            Port = _config.Port,
            VirtualHost = _config.VirtualHost
        };
    }
}

