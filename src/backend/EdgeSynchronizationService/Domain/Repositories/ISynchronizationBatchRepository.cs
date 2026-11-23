using EdgeSynchronizationService.Domain.Model.Aggregates;

namespace EdgeSynchronizationService.Domain.Repositories;

public interface ISynchronizationBatchRepository
{
    Task<SynchronizationBatch?> FindByBatchIdAsync(Guid batchId);
    Task<bool> TryAddAsync(SynchronizationBatch batch);
    Task UpdateAsync(SynchronizationBatch batch);

    /// <summary>Batches whose events are not in Kafka yet, oldest first.</summary>
    Task<IReadOnlyList<SynchronizationBatch>> FindPendingPublicationAsync(int limit);
}
