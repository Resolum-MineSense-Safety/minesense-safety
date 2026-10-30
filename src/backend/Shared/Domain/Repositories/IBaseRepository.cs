using MineSenseSafety.Shared.Domain.Model;

namespace MineSenseSafety.Shared.Domain.Repositories;

public interface IBaseRepository<TAggregate> where TAggregate : AggregateRoot
{
    Task AddAsync(TAggregate aggregate);
    Task<TAggregate?> FindByIdAsync(Guid id);
    Task<IEnumerable<TAggregate>> ListAsync();
    Task UpdateAsync(TAggregate aggregate);
}
