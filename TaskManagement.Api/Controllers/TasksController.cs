using Microsoft.AspNetCore.Mvc;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;

namespace TaskManagement.Api.Controllers;

/// <summary>REST controller for task management.</summary>
[ApiController]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _service;
    private readonly ILogger<TasksController> _logger;

    public TasksController(ITaskService service, ILogger<TasksController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Creates a new task.</summary>
    /// <param name="request">Task creation data.</param>
    /// <returns>The created task.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request)
    {
        _logger.LogInformation("Create task: Title={Title}", request.Title);
        var result = await _service.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Returns a task by id.</summary>
    /// <param name="id">Task identifier.</param>
    /// <returns>The task, or 404 if not found.</returns>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Get(long id)
    {
        _logger.LogInformation("Get task: Id={Id}", id);
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Returns all tasks.</summary>
    /// <returns>List of tasks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TaskDto>>> GetAll()
    {
        _logger.LogInformation("GetAll tasks");
        return Ok(await _service.GetAllAsync());
    }

    /// <summary>Updates task fields.</summary>
    /// <param name="id">Task identifier.</param>
    /// <param name="request">Updated task data.</param>
    /// <returns>The updated task, or 404 if not found.</returns>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Update(long id, [FromBody] UpdateTaskRequest request)
    {
        _logger.LogInformation("Update task: Id={Id}", id);
        var result = await _service.UpdateAsync(id, request);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Deletes a task.</summary>
    /// <param name="id">Task identifier.</param>
    /// <returns>204 No Content, or 404 if not found.</returns>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        _logger.LogInformation("Delete task: Id={Id}", id);
        var deleted = await _service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
