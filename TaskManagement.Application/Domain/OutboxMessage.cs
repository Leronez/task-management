namespace TaskManagement.Application.Domain;

/// <summary>Outgoing event pending publication to the message broker.</summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Event type: "TaskCreated", "TaskUpdated", or "TaskDeleted".</summary>
    public string EventType { get; set; } = null!;

    /// <summary>JSON-serialized event payload.</summary>
    public string Payload { get; set; } = null!;

    /// <summary>UTC time the message was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set when the message is successfully published to the broker.</summary>
    public DateTime? ProcessedAt { get; set; }
}
