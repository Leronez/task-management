using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Domain;

namespace TaskManagement.Infrastructure.Repositories;

/// <summary>EF Core implementation of the outbox repository (staging-only).</summary>
public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _db;

    public OutboxRepository(AppDbContext db) => _db = db;

    /// <inheritdoc/>
    public void Add(OutboxMessage message) => _db.OutboxMessages.Add(message);
}
