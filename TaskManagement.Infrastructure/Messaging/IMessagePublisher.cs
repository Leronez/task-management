namespace TaskManagement.Infrastructure.Messaging;

/// <summary>Abstraction for publishing raw JSON payloads to a message broker.</summary>
public interface IMessagePublisher
{
    /// <summary>Publishes a JSON payload to the broker.</summary>
    /// <param name="payload">JSON string to publish.</param>
    Task PublishAsync(string payload);
}
