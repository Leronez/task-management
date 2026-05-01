using TaskManagement.Application.Domain;

namespace TaskManagement.Application.Interfaces;

/// <summary>Task repository. All write methods only stage changes; persistence is handled by IUnitOfWork.</summary>
public interface ITaskRepository
{
    /// <summary>Stages a new task for insertion (does not save).</summary>
    /// <param name="task">The task to add.</param>
    void Add(TaskItem task);

    /// <summary>Stages task changes for update (does not save).</summary>
    /// <param name="task">The task to update.</param>
    void Update(TaskItem task);

    /// <summary>Stages a task for deletion (does not save).</summary>
    /// <param name="task">The task to delete.</param>
    void Delete(TaskItem task);

    /// <summary>Returns the task with the given id, or null if not found.</summary>
    /// <param name="id">Task identifier.</param>
    /// <returns>The task, or null if not found.</returns>
    Task<TaskItem?> GetById(long id);

    /// <summary>Returns all tasks.</summary>
    /// <returns>List of all tasks.</returns>
    Task<List<TaskItem>> GetAll();
}
