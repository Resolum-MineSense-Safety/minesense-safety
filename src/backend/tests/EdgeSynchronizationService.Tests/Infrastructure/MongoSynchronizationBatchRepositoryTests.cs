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

    [Fact]
    public void Snapshot_RoundTrip_KeepsThePublicationState()
    {
        // Arrange
        var pending = Batch(Guid.NewGuid());
        var published = Batch(Guid.NewGuid());
        published.MarkPublished(Now);

        // Act
        var restoredPending = AggregateSnapshot.Deserialize<SynchronizationBatch>(AggregateSnapshot.Serialize(pending));
        var restoredPublished = AggregateSnapshot.Deserialize<SynchronizationBatch>(AggregateSnapshot.Serialize(published));

        // Assert
        Assert.True(restoredPending.PublicationPending);
        Assert.Null(restoredPending.PublishedAt);
        Assert.False(restoredPublished.PublicationPending);
        Assert.Equal(Now, restoredPublished.PublishedAt);
    }

    [MongoFact]
    public async Task Mongo_RealServer_PendingBatchesSurviveARestartUntilPublished()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("edge_tests");
        var repository = new MongoSynchronizationBatchRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var batch = Batch(Guid.NewGuid());
        await repository.TryAddAsync(batch);

        // Act
        var restarted = new MongoSynchronizationBatchRepository(mongo.Reopen());
        var pending = await restarted.FindPendingPublicationAsync(10);
        pending[0].MarkPublished(Now);
        await restarted.UpdateAsync(pending[0]);

        // Assert
        Assert.Equal(batch.BatchId, Assert.Single(pending).BatchId);
        Assert.Empty(await restarted.FindPendingPublicationAsync(10));
        Assert.Equal(Now, (await restarted.FindByBatchIdAsync(batch.BatchId))!.PublishedAt);
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
