using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Repositories;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace EdgeSynchronizationService.Infrastructure.Persistence.MongoDb;

public class SynchronizationBatchDocument : MongoAggregateDocument
{
    public string BatchId { get; set; } = string.Empty;
    public string EdgeNodeId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public long SequenceNumber { get; set; }
    public DateTime ReceivedAt { get; set; }
}

/// <summary>
/// Batches synchronized from the Edge in MongoDB (ADR-005), with their events in the document. The
/// unique index on the batch id makes a resubmitted batch idempotent: <see cref="TryAddAsync"/>
/// returns false and the command service answers with the stored batch.
/// </summary>
public class MongoSynchronizationBatchRepository(IMongoDatabase database)
    : MongoRepository<SynchronizationBatch, SynchronizationBatchDocument>(database, "synchronization_batches"),
      ISynchronizationBatchRepository
{
    protected override SynchronizationBatchDocument CreateDocument(SynchronizationBatch batch) => new()
    {
        BatchId = batch.BatchId.ToString(),
        EdgeNodeId = batch.EdgeNodeId.ToString(),
        DeviceId = batch.DeviceId.ToString(),
        SequenceNumber = batch.SequenceNumber,
        ReceivedAt = Utc(batch.ReceivedAt)
    };

    protected override IEnumerable<CreateIndexModel<SynchronizationBatchDocument>> Indexes() =>
    [
        new(Keys.Ascending(item => item.BatchId), new CreateIndexOptions { Unique = true }),
        new(Keys.Ascending(item => item.EdgeNodeId).Ascending(item => item.DeviceId).Ascending(item => item.SequenceNumber))
    ];

    public async Task<SynchronizationBatch?> FindByBatchIdAsync(Guid batchId)
    {
        var document = await Collection.Find(item => item.BatchId == batchId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }

    public async Task<bool> TryAddAsync(SynchronizationBatch batch)
    {
        try
        {
            await AddAsync(batch);
            return true;
        }
        catch (DomainConflictException)
        {
            return false;
        }
    }
}
