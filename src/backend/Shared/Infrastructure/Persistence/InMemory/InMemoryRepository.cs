using System.Collections.Concurrent;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Domain.Repositories;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

/// <summary>
/// Thread-safe in-memory repository used while the PostgreSQL / MongoDB
/// adapters of each bounded context are implemented.
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
