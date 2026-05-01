using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Domain;

namespace TaskManagement.Application.Services;

/// <summary>Application service for task management.</summary>
public class TaskService : ITaskService
{
    private static readonly ActivitySource _activitySource = new("TaskManagement.Application");

    private readonly ITaskRepository _repo;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        ITaskRepository repo,
        IOutboxRepository outbox,
        IUnitOfWork uow,
        ILogger<TaskService> logger)
    {
        _repo = repo;
        _outbox = outbox;
        _uow = uow;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<TaskDto> CreateAsync(CreateTaskRequest dto)
    {
        using var activity = _activitySource.StartActivity("TaskService.CreateAsync");
        _logger.LogInformation("Creating task: Title={Title}", dto.Title);

        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Status = dto.Status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _repo.Add(task);
        StageEvent(task.Id, "TaskCreated");
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Task created: Id={Id}", task.Id);
        return Map(task);
    }

    /// <inheritdoc/>
    public async Task<TaskDto?> GetByIdAsync(long id)
    {
        using var activity = _activitySource.StartActivity("TaskService.GetByIdAsync");
        activity?.SetTag("task.id", id);
        _logger.LogInformation("Getting task: Id={Id}", id);
        var task = await _repo.GetById(id);
        return task == null ? null : Map(task);
    }

    /// <inheritdoc/>
    public async Task<List<TaskDto>> GetAllAsync()
    {
        using var activity = _activitySource.StartActivity("TaskService.GetAllAsync");
        _logger.LogInformation("Getting all tasks");
        var tasks = await _repo.GetAll();
        return tasks.Select(Map).ToList();
    }

    /// <inheritdoc/>
    public async Task<TaskDto?> UpdateAsync(long id, UpdateTaskRequest dto)
    {
        using var activity = _activitySource.StartActivity("TaskService.UpdateAsync");
        activity?.SetTag("task.id", id);
        _logger.LogInformation("Updating task: Id={Id}", id);

        var task = await _repo.GetById(id);
        if (task == null) return null;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.UpdatedAt = DateTime.UtcNow;

        _repo.Update(task);
        StageEvent(task.Id, "TaskUpdated");
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Task updated: Id={Id}", task.Id);
        return Map(task);
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(long id)
    {
        using var activity = _activitySource.StartActivity("TaskService.DeleteAsync");
        activity?.SetTag("task.id", id);
        _logger.LogInformation("Deleting task: Id={Id}", id);

        var task = await _repo.GetById(id);
        if (task == null) return false;

        _repo.Delete(task);
        StageEvent(task.Id, "TaskDeleted");
        await _uow.SaveChangesAsync();

        _logger.LogInformation("Task deleted: Id={Id}", id);
        return true;
    }

    private void StageEvent(long taskId, string eventType)
    {
        var payload = JsonSerializer.Serialize(new TaskEventDto
        {
            TaskId = taskId,
            EventType = eventType,
            OccurredAt = DateTime.UtcNow
        });

        _outbox.Add(new OutboxMessage
        {
            EventType = eventType,
            Payload = payload,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static TaskDto Map(TaskItem x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        Description = x.Description,
        Status = x.Status,
        CreatedAt = x.CreatedAt,
        UpdatedAt = x.UpdatedAt
    };
}
