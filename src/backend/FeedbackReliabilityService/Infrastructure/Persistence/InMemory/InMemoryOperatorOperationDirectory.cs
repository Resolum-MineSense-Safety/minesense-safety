using System.Collections.Concurrent;
using FeedbackReliabilityService.Application.Internal.OutboundServices;

namespace FeedbackReliabilityService.Infrastructure.Persistence.InMemory;

public class InMemoryOperatorOperationDirectory : IOperatorOperationDirectory
{
    private readonly ConcurrentDictionary<Guid, (Guid? OperationId, DateTimeOffset UpdatedAt)> _operators = new();

    public Task<Guid?> FindOperationIdAsync(Guid operatorId) =>
        Task.FromResult(_operators.TryGetValue(operatorId, out var entry) ? entry.OperationId : null);

    public Task UpdateAsync(Guid operatorId, Guid? operationId, DateTimeOffset updatedAt)
    {
        _operators.AddOrUpdate(operatorId, (operationId, updatedAt),
            (_, current) => updatedAt >= current.UpdatedAt ? (operationId, updatedAt) : current);
        return Task.CompletedTask;
    }
}
