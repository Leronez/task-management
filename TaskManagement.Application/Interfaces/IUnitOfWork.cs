namespace TaskManagement.Application.Interfaces;

/// <summary>Unit of Work: commits all staged changes in a single transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists all pending changes atomically.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
