using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace EdgeSynchronizationService.Tests.Domain;

public class SyncEventTests
{
    [Fact]
    public void CreatesPendingEventFromValidCommand()
    {
        var item = new SyncEvent(new EnqueueSyncEventCommand(
            Guid.NewGuid(), "truck-07", "telemetry",
            DateTimeOffset.UtcNow, """{"heartRate":82}"""));

        Assert.Equal(SyncEventStatus.Pending, item.Status);
        Assert.Equal(0, item.Attempts);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    public void RejectsInvalidPayload(string payload)
    {
        var command = new EnqueueSyncEventCommand(
            Guid.NewGuid(), "truck-07", "telemetry", DateTimeOffset.UtcNow, payload);

        Assert.Throws<DomainException>(() => new SyncEvent(command));
    }

    [Fact]
    public void MarksEventAsSynchronized()
    {
        var item = new SyncEvent(new EnqueueSyncEventCommand(
            Guid.NewGuid(), "truck-07", "telemetry",
            DateTimeOffset.UtcNow, "{}"));

        item.MarkSynchronized(DateTimeOffset.UtcNow);

        Assert.Equal(SyncEventStatus.Synchronized, item.Status);
        Assert.Equal(1, item.Attempts);
        Assert.Null(item.LastError);
    }
}
