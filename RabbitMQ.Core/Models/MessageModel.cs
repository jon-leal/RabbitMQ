namespace RabbitMQ.Core.Models;

public class MessageModel
{
    public string Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; }
}