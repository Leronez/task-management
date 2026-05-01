using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Application.Domain;
using TaskManagement.Infrastructure;
using TaskManagement.Infrastructure.Messaging;
using Xunit;

namespace TaskManagement.Tests.Outbox;

/// <summary>
/// Unit tests for OutboxProcessor using EF Core InMemory database.
/// </summary>
public class OutboxProcessorTests
{
    private static (AppDbContext db, IServiceScopeFactory scopeFactory) CreateInMemoryDb()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseInMemoryDatabase(dbName));

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<AppDbContext>();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        return (db, scopeFactory);
    }

    private static OutboxProcessor CreateProcessor(
        IServiceScopeFactory scopeFactory,
        IMessagePublisher publisher)
        => new(scopeFactory, publisher, Mock.Of<ILogger<OutboxProcessor>>());

    [Fact]
    public async Task ProcessBatch_UnprocessedMessage_IsPublished()
    {
        var (db, scopeFactory) = CreateInMemoryDb();
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "TaskCreated",
            Payload = """{"TaskId":1,"EventType":"TaskCreated"}""",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var publisherMock = new Mock<IMessagePublisher>();
        var processor = CreateProcessor(scopeFactory, publisherMock.Object);

        await processor.ProcessBatchAsync(CancellationToken.None);

        publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_UnprocessedMessage_IsMarkedAsProcessed()
    {
        var (db, scopeFactory) = CreateInMemoryDb();
        var msg = new OutboxMessage
        {
            EventType = "TaskCreated",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow
        };
        db.OutboxMessages.Add(msg);
        await db.SaveChangesAsync();

        var processor = CreateProcessor(scopeFactory, Mock.Of<IMessagePublisher>());
        await processor.ProcessBatchAsync(CancellationToken.None);

        db.ChangeTracker.Clear();
        var updated = await db.OutboxMessages.FindAsync(msg.Id);
        Assert.NotNull(updated!.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatch_MultipleMessages_AllPublishedInOrder()
    {
        var (db, scopeFactory) = CreateInMemoryDb();

        var base_time = DateTime.UtcNow;
        db.OutboxMessages.AddRange(
            new OutboxMessage { EventType = "TaskCreated", Payload = "1", CreatedAt = base_time },
            new OutboxMessage { EventType = "TaskUpdated", Payload = "2", CreatedAt = base_time.AddSeconds(1) },
            new OutboxMessage { EventType = "TaskDeleted", Payload = "3", CreatedAt = base_time.AddSeconds(2) });
        await db.SaveChangesAsync();

        var published = new List<string>();
        var publisherMock = new Mock<IMessagePublisher>();
        publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>()))
                     .Callback<string>(payload => published.Add(payload))
                     .Returns(Task.CompletedTask);

        var processor = CreateProcessor(scopeFactory, publisherMock.Object);
        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal(3, published.Count);
        Assert.Equal("1", published[0]);
        Assert.Equal("2", published[1]);
        Assert.Equal("3", published[2]);
    }

    [Fact]
    public async Task ProcessBatch_AlreadyProcessedMessage_IsSkipped()
    {
        var (db, scopeFactory) = CreateInMemoryDb();
        db.OutboxMessages.Add(new OutboxMessage
        {
            EventType = "TaskCreated",
            Payload = "{}",
            CreatedAt = DateTime.UtcNow.AddMinutes(-1),
            ProcessedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var publisherMock = new Mock<IMessagePublisher>();
        var processor = CreateProcessor(scopeFactory, publisherMock.Object);

        await processor.ProcessBatchAsync(CancellationToken.None);

        publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_EmptyQueue_PublisherNotCalled()
    {
        var (_, scopeFactory) = CreateInMemoryDb();

        var publisherMock = new Mock<IMessagePublisher>();
        var processor = CreateProcessor(scopeFactory, publisherMock.Object);

        await processor.ProcessBatchAsync(CancellationToken.None);

        publisherMock.Verify(p => p.PublishAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessBatch_MixedMessages_OnlyPublishesUnprocessed()
    {
        var (db, scopeFactory) = CreateInMemoryDb();
        db.OutboxMessages.AddRange(
            new OutboxMessage { EventType = "Old", Payload = "old", CreatedAt = DateTime.UtcNow.AddMinutes(-5), ProcessedAt = DateTime.UtcNow.AddMinutes(-4) },
            new OutboxMessage { EventType = "New", Payload = "new", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var published = new List<string>();
        var publisherMock = new Mock<IMessagePublisher>();
        publisherMock.Setup(p => p.PublishAsync(It.IsAny<string>()))
                     .Callback<string>(p => published.Add(p))
                     .Returns(Task.CompletedTask);

        var processor = CreateProcessor(scopeFactory, publisherMock.Object);
        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Single(published);
        Assert.Equal("new", published[0]);
    }
}
