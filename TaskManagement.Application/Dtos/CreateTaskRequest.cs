using System.ComponentModel.DataAnnotations;
using TaskManagement.Application.Domain;
using TaskStatus = TaskManagement.Application.Domain.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>Request payload for creating a new task.</summary>
public class CreateTaskRequest
{
    /// <summary>Task title (required, 1-200 characters).</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    /// <summary>Optional task description (max 2000 characters).</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Initial status. Defaults to New.</summary>
    public TaskStatus Status { get; set; } = TaskStatus.New;
}
