using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Queries;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;

namespace EdgeSynchronizationService.Application.Internal.QueryServices;

public sealed class SynchronizationQueryService(ISynchronizationBatchRepository repository)
    : ISynchronizationQueryService
{
    public Task<SynchronizationBatch?> Handle(GetSynchronizationBatchQuery query) =>
        repository.FindByBatchIdAsync(query.BatchId);
}
