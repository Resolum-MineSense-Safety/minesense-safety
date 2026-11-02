using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace AnalyticsService.Infrastructure.Persistence.MongoDb;

/// <summary>Id = operator id. <see cref="UpdatedAtTicks"/> compares versions with full precision.</summary>
public class OperatorContextDocument : MongoAggregateDocument
{
    public long UpdatedAtTicks { get; set; }
}

/// <summary>
/// Local copy of where each operator works (fed by FleetOperatorUpdated), in MongoDB (ADR-005).
/// The upsert only replaces an older or equal version. When a newer one is stored the filter does not
/// match, the insert collides on the same id and the stale update is ignored, as in memory.
/// </summary>
public class MongoOperatorContextRepository(IMongoDatabase database) : IOperatorContextRepository, IMongoCollectionInitializer
{
    private readonly IMongoCollection<OperatorContextDocument> _collection =
        database.GetCollection<OperatorContextDocument>("operator_contexts");

    /// <summary>Lookups are by id only: the default _id index is enough.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<OperatorContext?> FindByOperatorIdAsync(Guid operatorId)
    {
        var document = await _collection.Find(item => item.Id == operatorId.ToString()).FirstOrDefaultAsync();
        return document is null ? null : AggregateSnapshot.Deserialize<OperatorContext>(document.Payload);
    }

    public async Task UpsertAsync(OperatorContext context)
    {
        var id = context.OperatorId.ToString();
        var ticks = context.UpdatedAt.UtcTicks;
        var document = new OperatorContextDocument
        {
            Id = id, UpdatedAtTicks = ticks, Payload = AggregateSnapshot.Serialize(context)
        };
        try
        {
            await _collection.ReplaceOneAsync(item => item.Id == id && item.UpdatedAtTicks <= ticks, document,
                new ReplaceOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // A newer context of the operator is already stored.
        }
    }
}
