using System.Collections.Concurrent;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Domain.Repositories;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository, used when the service has no database configured
/// (tests, a bare <c>dotnet run</c>); otherwise the PostgreSQL or MongoDB adapter of the context is used.
/// </summary>
public abstract class InMemoryRepository<TAggregate> : IBaseRepository<TAggregate>
    where TAggregate : AggregateRoot
{
    protected readonly ConcurrentDictionary<Guid, TAggregate> Store = new();

    public Task AddAsync(TAggregate aggregate)
    {
        Store[aggregate.Id] = aggregate;
        return Task.CompletedTask;
    }

    public Task<TAggregate?> FindByIdAsync(Guid id) =>
        Task.FromResult(Store.GetValueOrDefault(id));

    public Task<IEnumerable<TAggregate>> ListAsync() =>
        Task.FromResult<IEnumerable<TAggregate>>(Store.Values.ToList());

    public Task UpdateAsync(TAggregate aggregate)
    {
        Store[aggregate.Id] = aggregate;
        return Task.CompletedTask;
    }
}
