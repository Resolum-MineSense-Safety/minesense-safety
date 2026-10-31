using System.Collections.Concurrent;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Repositories;

namespace AnalyticsService.Infrastructure.Persistence.InMemory;

public class InMemoryOperatorContextRepository : IOperatorContextRepository
{
    private readonly ConcurrentDictionary<Guid, OperatorContext> _contexts = new();

    public Task<OperatorContext?> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult(_contexts.GetValueOrDefault(operatorId));

    public Task UpsertAsync(OperatorContext context)
    {
        _contexts.AddOrUpdate(context.OperatorId, context,
            (_, current) => context.UpdatedAt >= current.UpdatedAt ? context : current);
        return Task.CompletedTask;
    }
}
