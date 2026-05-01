using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Domain;

namespace TaskManagement.Infrastructure.Repositories;

/// <summary>EF Core implementation of the task repository (staging-only — no SaveChanges).</summary>
public class TaskRepository : ITaskRepository
{
    private static readonly ActivitySource _activitySource = new("TaskManagement.Infrastructure");

    private readonly AppDbContext _db;

    public TaskRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public void Add(TaskItem task)
    {
        using var activity = _activitySource.StartActivity("TaskRepository.Add");
        _db.Tasks.Add(task);
    }

    /// <inheritdoc/>
    public void Update(TaskItem task)
    {
        using var activity = _activitySource.StartActivity("TaskRepository.Update");
        _db.Tasks.Update(task);
    }

    /// <inheritdoc/>
    public void Delete(TaskItem task)
    {
        using var activity = _activitySource.StartActivity("TaskRepository.Delete");
        _db.Tasks.Remove(task);
    }

    /// <inheritdoc/>
    public async Task<TaskItem?> GetById(long id)
    {
        using var activity = _activitySource.StartActivity("TaskRepository.GetById");
        activity?.SetTag("task.id", id);
        return await _db.Tasks
            .TagWith("TaskRepository.GetById")
            .Where(t => t.Id == id)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc/>
    public async Task<List<TaskItem>> GetAll()
    {
        using var activity = _activitySource.StartActivity("TaskRepository.GetAll");
        return await _db.Tasks
            .TagWith("TaskRepository.GetAll")
            .ToListAsync();
    }
}
