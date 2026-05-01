using TaskManagement.Application.DTOs;

namespace TaskManagement.Application.Interfaces;

/// <summary>Application service for managing tasks.</summary>
public interface ITaskService
{
    /// <summary>Creates a new task.</summary>
    /// <param name="dto">Task creation data.</param>
    /// <returns>The created task DTO.</returns>
    Task<TaskDto> CreateAsync(CreateTaskRequest dto);

    /// <summary>Returns the task with the given id, or null if not found.</summary>
    /// <param name="id">Task identifier.</param>
    /// <returns>The task DTO, or null if not found.</returns>
    Task<TaskDto?> GetByIdAsync(long id);

    /// <summary>Returns all tasks.</summary>
    /// <returns>List of all task DTOs.</returns>
    Task<List<TaskDto>> GetAllAsync();

    /// <summary>Updates task fields. Returns null if the task does not exist.</summary>
    /// <param name="id">Task identifier.</param>
    /// <param name="dto">Updated task data.</param>
    /// <returns>The updated task DTO, or null if not found.</returns>
    Task<TaskDto?> UpdateAsync(long id, UpdateTaskRequest dto);

    /// <summary>Deletes a task. Returns false if the task does not exist.</summary>
    /// <param name="id">Task identifier.</param>
    /// <returns>True if deleted; false if not found.</returns>
    Task<bool> DeleteAsync(long id);
}
