using System.Text.Json;
using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace EdgeSynchronizationService.Tests.Infrastructure;

public class MongoSynchronizationBatchRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 6, 0, 0, TimeSpan.Zero);

    private static SynchronizationBatch Batch(Guid batchId) =>
        new(new SubmitSynchronizationBatchCommand(batchId, Guid.NewGuid(), Guid.NewGuid(), 42, Now.AddMinutes(-5),
        [
            new SynchronizationEvent(Guid.NewGuid(), Now.AddMinutes(-6), "FatigueAssessed",
                JsonDocument.Parse("""{"riskLevel":"Warning","perclos":0.31}""").RootElement)
        ]), Now);

    [Fact]
    public void Snapshot_RoundTrip_KeepsEventsAndTheirJsonPayload()
    {
        // Arrange
        var batch = Batch(Guid.NewGuid());

        // Act
        var restored = AggregateSnapshot.Deserialize<SynchronizationBatch>(AggregateSnapshot.Serialize(batch));

        // Assert
        var stored = Assert.Single(restored.Events);
        Assert.Equal(batch.Events.Single().EventId, stored.EventId);
        Assert.Equal("Warning", stored.Payload.GetProperty("riskLevel").GetString());
        Assert.Equal(batch.SequenceNumber, restored.SequenceNumber);
    }

    [MongoFact]
    public async Task Mongo_RealServer_ResubmittedBatchIsIdempotentAcrossRestarts()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("edge_tests");
        var repository = new MongoSynchronizationBatchRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var batchId = Guid.NewGuid();
        var batch = Batch(batchId);

        // Act
        var first = await repository.TryAddAsync(batch);
        var restarted = new MongoSynchronizationBatchRepository(mongo.Reopen());
        var again = await restarted.TryAddAsync(Batch(batchId));
        var stored = await restarted.FindByBatchIdAsync(batchId);

        // Assert
        Assert.True(first);
        Assert.False(again);
        Assert.Equal(batch.Id, stored!.Id);
        Assert.Equal(0.31, Assert.Single(stored.Events).Payload.GetProperty("perclos").GetDouble());
        Assert.Null(await restarted.FindByBatchIdAsync(Guid.NewGuid()));
    }
}
