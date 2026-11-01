using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;
using MineSenseSafety.Shared.Tests.Domain.Model;
using MineSenseSafety.Tests.Support;
using MongoDB.Driver;

namespace MineSenseSafety.Shared.Tests.Infrastructure;

public class MongoPersistenceTests
{
    [Fact]
    public void AggregateSnapshot_RestoresPrivateFieldsCollectionsAndValueObjects()
    {
        // Arrange
        var sample = new SampleAggregate("Line 1", new SampleValue(0.25, "night"),
            new DateTimeOffset(2026, 10, 30, 7, 15, 0, 123, TimeSpan.FromHours(-5)).AddTicks(9));
        sample.Record("first");
        sample.Record("second");

        // Act
        var restored = AggregateSnapshot.Deserialize<SampleAggregate>(AggregateSnapshot.Serialize(sample));

        // Assert
        Assert.Equal(sample.Id, restored.Id);
        Assert.Equal("Line 1", restored.Name);
        Assert.Equal(sample.Value, restored.Value);
        Assert.Equal(sample.At.Ticks, restored.At.Ticks);
        Assert.Equal(sample.At.Offset, restored.At.Offset);
        Assert.Equal(new[] { "first", "second" }, restored.Entries);
        Assert.Equal(SampleState.Recorded, restored.State);
    }

    [Fact]
    public void AddMineSenseMongo_WithoutConnectionString_KeepsInMemoryStore()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var configured = services.AddMineSenseMongo(configuration, "minesense_tests");
        services.AddMineSenseEvents(configuration, "tests");

        // Assert
        Assert.False(configured);
        Assert.IsType<InMemoryProcessedEventStore>(services.BuildServiceProvider().GetRequiredService<IProcessedEventStore>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddMineSenseMongo_WithConnectionString_UsesMongoStoreAndDatabase(bool eventsFirst)
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:MongoDb"] = "mongodb://127.0.0.1:1",
            ["MongoDb:Database"] = "minesense_custom"
        }).Build();

        // Act
        if (eventsFirst) services.AddMineSenseEvents(configuration, "tests");
        var configured = services.AddMineSenseMongo(configuration, "minesense_tests");
        if (!eventsFirst) services.AddMineSenseEvents(configuration, "tests");
        var provider = services.BuildServiceProvider();

        // Assert
        Assert.True(configured);
        Assert.IsType<MongoProcessedEventStore>(provider.GetRequiredService<IProcessedEventStore>());
        Assert.Equal("minesense_custom", provider.GetRequiredService<IMongoDatabase>().DatabaseNamespace.DatabaseName);
        Assert.Single(provider.GetServices<IMongoCollectionInitializer>());
    }

    [MongoFact]
    public async Task MongoProcessedEventStore_RealServer_RemembersEventsAcrossInstances()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("shared_tests");
        var store = new MongoProcessedEventStore(mongo.Database);
        await store.InitializeAsync(CancellationToken.None);
        var eventId = Guid.NewGuid();

        // Act
        store.MarkProcessed(eventId);
        store.MarkProcessed(eventId);
        var restarted = new MongoProcessedEventStore(mongo.Reopen());

        // Assert
        Assert.True(restarted.HasProcessed(eventId));
        Assert.False(restarted.HasProcessed(Guid.NewGuid()));
        var indexes = await (await mongo.Database.GetCollection<ProcessedEventDocument>(MongoProcessedEventStore.CollectionName)
            .Indexes.ListAsync()).ToListAsync();
        Assert.Contains(indexes, index => index.Contains("expireAfterSeconds"));
    }
}
