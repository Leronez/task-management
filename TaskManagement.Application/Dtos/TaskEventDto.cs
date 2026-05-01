namespace TaskManagement.Application.DTOs;

/// <summary>Event payload serialized into the outbox and published to the broker.</summary>
public class TaskEventDto
{
    public long TaskId { get; set; }

    /// <summary>Event type: "TaskCreated", "TaskUpdated", or "TaskDeleted".</summary>
    public string EventType { get; set; } = null!;

    public DateTime OccurredAt { get; set; }
}
