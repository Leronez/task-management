using System.ComponentModel.DataAnnotations;
using TaskManagement.Application.Domain;
using TaskStatus = TaskManagement.Application.Domain.TaskStatus;

namespace TaskManagement.Application.DTOs;

/// <summary>Request payload for updating an existing task.</summary>
public class UpdateTaskRequest
{
    /// <summary>Updated title (1-200 characters).</summary>
    [Required]
    [MinLength(1)]
    [MaxLength(200)]
    public string Title { get; set; } = default!;

    /// <summary>Updated description (max 2000 characters).</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    public TaskStatus Status { get; set; }
}
