using RabbitMQ.Client;

namespace RabbitMQ.Core.Interfaces
{
    public interface IRabbitMQService
    {
        // Publicação simples apenas com fila
        void Publish(string queueName, string message);
    
        // Publicação com Exchange, DLQ e Routing
        void Publish(string exchange, string deadLetterExchange, string queue, string routingKey, string deadLetterRoutingKey, string message, IBasicProperties properties = null);
        
        // Abordagem 2: Usando genérico <T> aceita model e serialização automática dentro do serviço
        // void Publish<T>(string queueName, T message);
        // void Publish<T>(string exchange, string deadLetterExchange, string queue, 
        //     string routingKey, string deadLetterRoutingKey, T message, 
        //     IBasicProperties properties = null);
    }
}