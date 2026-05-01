using TaskManagement.Application.Domain;
using TaskStatus = TaskManagement.Application.Domain.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>Read-only representation of a task returned by the API.</summary>
public class TaskDto
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
