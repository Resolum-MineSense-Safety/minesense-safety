using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Queries;

namespace EdgeSynchronizationService.Domain.Services;

public interface ISynchronizationQueryService
{
    Task<SynchronizationBatch?> Handle(GetSynchronizationBatchQuery query);
}
