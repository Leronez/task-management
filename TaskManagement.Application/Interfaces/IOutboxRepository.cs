using TaskManagement.Application.Domain;

namespace TaskManagement.Application.Interfaces;

/// <summary>Repository for staging outbox messages (no save — committed via IUnitOfWork).</summary>
public interface IOutboxRepository
{
    /// <summary>Stages an outbox message for the current unit of work.</summary>
    /// <param name="message">The outbox message to stage.</param>
    void Add(OutboxMessage message);
}
