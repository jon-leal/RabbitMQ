# RabbitMQ Implementation Reference

Este projeto serve como referência para implementação de mensageria usando RabbitMQ em .NET. Contém exemplos de publicação de mensagens com e sem Dead Letter Queue (DLQ).

## Requisitos

- .NET 8.0
- RabbitMQ.Client 6.8.1
- RabbitMQ Server instalado e rodando

## Configurações Adicionais

### Configurações de Resiliência
Para melhorar a resiliência da conexão com RabbitMQ, adicione as seguintes configurações:

```csharp
var factory = new ConnectionFactory
{
    // Configurações avançadas para melhor resiliência
    AutomaticRecoveryEnabled = true,
    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
    RequestedHeartbeat = TimeSpan.FromSeconds(60)
};
```
-----------------------------------------------------------------
### Processamento Paralelo de Mensagens
Para otimizar o processamento de grande volume de mensagens em paralelo, configure o consumer com as seguintes alterações:
```csharp

public class MessageConsumerWorker : BackgroundService
{
    private readonly ILogger<MessageConsumerWorker> _logger;
    private readonly RabbitMQConfig _config;
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    
    // Define número máximo de tarefas concorrentes
    private readonly int _maxConcurrentTasks = 15; // Ou mais 20,30, verificar consumos RAM e CPU
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Configura o número de mensagens que o worker pode processar simultaneamente
        channel.BasicQos(
            prefetchSize: 0,                              // Sem limite de tamanho
            prefetchCount: (ushort)_maxConcurrentTasks,   // Limite de mensagens simultâneas
            global: false                                 // Aplica por consumer, não por canal
        );

        // Executa o processamento de mensagens em paralelo usando Task.Run
        consumer.Received += async (model, ea) => 
            _ = Task.Run(async () => await ProcessMessageAsync(ea, channel));
    }
}
```
-----------------------------------------------------------------    
🤝 Contribuição
Sinta-se à vontade para contribuir com melhorias ou correções.



