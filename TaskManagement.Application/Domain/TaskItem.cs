namespace TaskManagement.Application.Domain;

public enum TaskStatus
{
    New = 0,
    InProgress = 1,
    Completed = 2
}

/// <summary>Core domain entity representing a user task.</summary>
public class TaskItem
{
    public long Id { get; set; }

    /// <summary>Max 200 characters.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Max 2000 characters.</summary>
    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.New;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
