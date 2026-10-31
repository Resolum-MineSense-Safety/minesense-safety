using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MongoDB.Driver;

namespace MineSenseSafety.Shared.Infrastructure.Messaging;

/// <summary>
/// Processed event ids kept in the MongoDB database of the service, so a redelivery after a restart
/// is still ignored. Ids expire after <see cref="Retention"/> (TTL index); older redeliveries are
/// absorbed by the idempotent business keys of each handler, as with the in-memory store.
/// </summary>
public sealed class MongoProcessedEventStore(IMongoDatabase database) : IProcessedEventStore, IMongoCollectionInitializer
{
    public const string CollectionName = "processed_events";
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);

    private readonly IMongoCollection<ProcessedEventDocument> _collection =
        database.GetCollection<ProcessedEventDocument>(CollectionName);

    public Task InitializeAsync(CancellationToken cancellationToken) => _collection.Indexes.CreateOneAsync(
        new CreateIndexModel<ProcessedEventDocument>(Builders<ProcessedEventDocument>.IndexKeys.Ascending(item => item.ProcessedAt),
            new CreateIndexOptions { ExpireAfter = Retention }),
        cancellationToken: cancellationToken);

    public bool HasProcessed(Guid eventId) =>
        _collection.Find(item => item.Id == eventId.ToString()).Limit(1).Any();

    public void MarkProcessed(Guid eventId)
    {
        try
        {
            _collection.InsertOne(new ProcessedEventDocument { Id = eventId.ToString(), ProcessedAt = DateTime.UtcNow });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Already recorded by a previous delivery: nothing to do.
        }
    }
}

public class ProcessedEventDocument
{
    public string Id { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
}
