using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Domain.Repositories;
using MongoDB.Driver;

namespace MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;

/// <summary>Creates the indexes of a collection when the service starts.</summary>
public interface IMongoCollectionInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Persistence document of an aggregate: the identity, the fields its queries and indexes need and
/// the exact <see cref="AggregateSnapshot"/> of its state. The domain class never sees MongoDB.
/// </summary>
public abstract class MongoAggregateDocument
{
    public string Id { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}

/// <summary>
/// MongoDB adapter of <see cref="IBaseRepository{TAggregate}"/> (ADR-005), with the semantics of the
/// in-memory repositories it replaces: add inserts, update replaces (or inserts) by id. A write that
/// breaks a unique index (same id or same business key, e.g. one alert per assessment) is reported
/// as a <see cref="DomainConflictException"/>, so a racing duplicate never gets stored.
/// </summary>
public abstract class MongoRepository<TAggregate, TDocument>(IMongoDatabase database, string collectionName)
    : IBaseRepository<TAggregate>, IMongoCollectionInitializer
    where TAggregate : AggregateRoot
    where TDocument : MongoAggregateDocument
{
    protected IMongoCollection<TDocument> Collection { get; } = database.GetCollection<TDocument>(collectionName);

    protected static FilterDefinitionBuilder<TDocument> Filter => Builders<TDocument>.Filter;
    protected static IndexKeysDefinitionBuilder<TDocument> Keys => Builders<TDocument>.IndexKeys;

    /// <summary>Document with the query fields of the aggregate; id and payload are filled in here.</summary>
    protected abstract TDocument CreateDocument(TAggregate aggregate);

    /// <summary>Indexes the queries of the repository need.</summary>
    protected virtual IEnumerable<CreateIndexModel<TDocument>> Indexes() => [];

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var indexes = Indexes().ToList();
        if (indexes.Count > 0)
            await Collection.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    public async Task AddAsync(TAggregate aggregate)
    {
        try
        {
            await Collection.InsertOneAsync(ToDocument(aggregate));
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw Duplicate();
        }
    }

    public async Task<TAggregate?> FindByIdAsync(Guid id)
    {
        var document = await Collection.Find(item => item.Id == id.ToString()).FirstOrDefaultAsync();
        return document is null ? null : ToEntity(document);
    }

    public async Task<IEnumerable<TAggregate>> ListAsync() => await ToEntitiesAsync(Collection.Find(Filter.Empty));

    public virtual async Task UpdateAsync(TAggregate aggregate)
    {
        try
        {
            await Collection.ReplaceOneAsync(item => item.Id == aggregate.Id.ToString(), ToDocument(aggregate),
                new ReplaceOptions { IsUpsert = true });
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw Duplicate();
        }
    }

    protected TDocument ToDocument(TAggregate aggregate)
    {
        var document = CreateDocument(aggregate);
        document.Id = aggregate.Id.ToString();
        document.Payload = AggregateSnapshot.Serialize(aggregate);
        return document;
    }

    protected static TAggregate ToEntity(TDocument document) => AggregateSnapshot.Deserialize<TAggregate>(document.Payload);

    protected static async Task<IReadOnlyList<TAggregate>> ToEntitiesAsync(IFindFluent<TDocument, TDocument> find)
    {
        var documents = await find.ToListAsync();
        return documents.Select(ToEntity).ToList();
    }

    /// <summary>MongoDB keeps milliseconds: stored times are normalized to UTC for range queries.</summary>
    protected static DateTime Utc(DateTimeOffset moment) => moment.UtcDateTime;

    /// <summary>Case-insensitive key for equality filters (the in-memory repositories ignore case).</summary>
    protected static string? Key(string? value) => value?.Trim().ToUpperInvariant();

    private static DomainConflictException Duplicate() =>
        new($"A {typeof(TAggregate).Name} with the same identity or business key already exists.");
}
