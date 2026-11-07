using EdgeSynchronizationService.Domain.Model.Aggregates;

namespace EdgeSynchronizationService.Domain.Repositories;

public interface ISynchronizationBatchRepository
{
    Task<SynchronizationBatch?> FindByBatchIdAsync(Guid batchId);
    Task<bool> TryAddAsync(SynchronizationBatch batch);
}
