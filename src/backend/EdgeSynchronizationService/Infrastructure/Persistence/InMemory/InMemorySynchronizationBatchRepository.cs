using System.Collections.Concurrent;
using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Repositories;

namespace EdgeSynchronizationService.Infrastructure.Persistence.InMemory;

public sealed class InMemorySynchronizationBatchRepository : ISynchronizationBatchRepository
{
    private readonly ConcurrentDictionary<Guid, SynchronizationBatch> _batches = new();

    public Task<SynchronizationBatch?> FindByBatchIdAsync(Guid batchId) =>
        Task.FromResult(_batches.TryGetValue(batchId, out var batch) ? batch : null);

    public Task<bool> TryAddAsync(SynchronizationBatch batch) =>
        Task.FromResult(_batches.TryAdd(batch.BatchId, batch));

    public Task UpdateAsync(SynchronizationBatch batch)
    {
        _batches[batch.BatchId] = batch;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SynchronizationBatch>> FindPendingPublicationAsync(int limit) =>
        Task.FromResult<IReadOnlyList<SynchronizationBatch>>(_batches.Values
            .Where(batch => batch.PublicationPending)
            .OrderBy(batch => batch.ReceivedAt).ThenBy(batch => batch.SequenceNumber)
            .Take(limit).ToList());
}
