using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Application.DTOs;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;
using TaskManagement.Application.Domain;
using Xunit;
using TaskStatus = TaskManagement.Application.Domain.TaskStatus;

namespace TaskManagement.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _repoMock = new();
    private readonly Mock<IOutboxRepository> _outboxMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<TaskService>> _loggerMock = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(
            _repoMock.Object,
            _outboxMock.Object,
            _uowMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_ReturnsDto()
    {
        var request = new CreateTaskRequest { Title = "Test Task", Description = "Desc" };

        _repoMock.Setup(r => r.Add(It.IsAny<TaskItem>()))
                 .Callback<TaskItem>(t => t.Id = 42);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(request);

        Assert.Equal(42, result.Id);
        Assert.Equal("Test Task", result.Title);
        Assert.Equal("Desc", result.Description);
        Assert.Equal(TaskStatus.New, result.Status);
    }

    [Fact]
    public async Task CreateAsync_WritesOutboxMessage()
    {
        _repoMock.Setup(r => r.Add(It.IsAny<TaskItem>()));
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateTaskRequest { Title = "T" });

        _outboxMock.Verify(o => o.Add(It.Is<OutboxMessage>(m => m.EventType == "TaskCreated")), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_CallsSaveChangesOnce()
    {
        _repoMock.Setup(r => r.Add(It.IsAny<TaskItem>()));
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateTaskRequest { Title = "T" });

        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsDto()
    {
        var task = new TaskItem { Id = 1, Title = "My Task", Status = TaskStatus.InProgress };
        _repoMock.Setup(r => r.GetById(1)).ReturnsAsync(task);

        var result = await _sut.GetByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        Assert.Equal("My Task", result.Title);
        Assert.Equal(TaskStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((TaskItem?)null);

        var result = await _sut.GetByIdAsync(99);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedList()
    {
        var tasks = new List<TaskItem>
        {
            new() { Id = 1, Title = "Task A" },
            new() { Id = 2, Title = "Task B" }
        };
        _repoMock.Setup(r => r.GetAll()).ReturnsAsync(tasks);

        var result = await _sut.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("Task A", result[0].Title);
        Assert.Equal("Task B", result[1].Title);
    }

    [Fact]
    public async Task DeleteAsync_ExistingId_ReturnsTrueAndWritesOutbox()
    {
        var task = new TaskItem { Id = 5, Title = "To Delete" };
        _repoMock.Setup(r => r.GetById(5)).ReturnsAsync(task);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var result = await _sut.DeleteAsync(5);

        Assert.True(result);
        _repoMock.Verify(r => r.Delete(task), Times.Once);
        _outboxMock.Verify(o => o.Add(It.Is<OutboxMessage>(m => m.EventType == "TaskDeleted")), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFalse()
    {
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((TaskItem?)null);

        var result = await _sut.DeleteAsync(99);

        Assert.False(result);
        _repoMock.Verify(r => r.Delete(It.IsAny<TaskItem>()), Times.Never);
        _outboxMock.Verify(o => o.Add(It.IsAny<OutboxMessage>()), Times.Never);
        _uowMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ExistingId_UpdatesAndReturnsDto()
    {
        var task = new TaskItem { Id = 3, Title = "Old", Status = TaskStatus.New, UpdatedAt = DateTime.UtcNow.AddDays(-1) };
        _repoMock.Setup(r => r.GetById(3)).ReturnsAsync(task);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await _sut.UpdateAsync(3, new UpdateTaskRequest
        {
            Title = "New Title",
            Description = "New Desc",
            Status = TaskStatus.InProgress
        });

        Assert.NotNull(result);
        Assert.Equal("New Title", result!.Title);
        Assert.Equal(TaskStatus.InProgress, result.Status);
        Assert.True(task.UpdatedAt >= before);

        _repoMock.Verify(r => r.Update(task), Times.Once);
        _outboxMock.Verify(o => o.Add(It.Is<OutboxMessage>(m => m.EventType == "TaskUpdated")), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NonExistingId_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetById(99)).ReturnsAsync((TaskItem?)null);

        var result = await _sut.UpdateAsync(99, new UpdateTaskRequest { Title = "X" });

        Assert.Null(result);
        _repoMock.Verify(r => r.Update(It.IsAny<TaskItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_OutboxPayload_ContainsCorrectTaskIdAndEventType()
    {
        OutboxMessage? captured = null;
        _repoMock.Setup(r => r.Add(It.IsAny<TaskItem>()))
                 .Callback<TaskItem>(t => t.Id = 7);
        _outboxMock.Setup(o => o.Add(It.IsAny<OutboxMessage>()))
                   .Callback<OutboxMessage>(m => captured = m);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateTaskRequest { Title = "Payload Test" });

        Assert.NotNull(captured);
        Assert.Equal("TaskCreated", captured!.EventType);

        var dto = JsonSerializer.Deserialize<TaskEventDto>(captured.Payload);
        Assert.NotNull(dto);
        Assert.Equal(7, dto!.TaskId);
        Assert.Equal("TaskCreated", dto.EventType);
        Assert.True(dto.OccurredAt > DateTime.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task DeleteAsync_OutboxPayload_ContainsDeletedEventType()
    {
        var task = new TaskItem { Id = 9, Title = "To Delete" };
        _repoMock.Setup(r => r.GetById(9)).ReturnsAsync(task);
        OutboxMessage? captured = null;
        _outboxMock.Setup(o => o.Add(It.IsAny<OutboxMessage>()))
                   .Callback<OutboxMessage>(m => captured = m);
        _uowMock.Setup(u => u.SaveChangesAsync(default)).Returns(Task.CompletedTask);

        await _sut.DeleteAsync(9);

        Assert.NotNull(captured);
        var dto = JsonSerializer.Deserialize<TaskEventDto>(captured!.Payload);
        Assert.Equal("TaskDeleted", dto!.EventType);
        Assert.Equal(9, dto.TaskId);
    }
}
