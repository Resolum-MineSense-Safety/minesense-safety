using System.Text.Json;
using EdgeSynchronizationService.Application.Internal.CommandServices;
using EdgeSynchronizationService.Application.Internal.IntegrationEvents;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace EdgeSynchronizationService.Tests.Application;

public class EdgeEventPublicationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Captured = Now.AddHours(-3);

    private readonly InMemorySynchronizationBatchRepository _repository = new();
    private readonly FakeConfirmedPublisher _publisher = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly SynchronizationCommandService _commandService;
    private readonly EdgeEventPublicationService _publication;
    private readonly Guid _deviceId = Guid.NewGuid();

    public EdgeEventPublicationServiceTests()
    {
        _commandService = new SynchronizationCommandService(_repository, _time);
        _publication = new EdgeEventPublicationService(_repository, _publisher, _time);
    }

    private static SynchronizationEvent Event(string type, int minute, string payload = """{"operatorId":"x"}""") =>
        new(Guid.NewGuid(), Captured.AddMinutes(minute), type, JsonDocument.Parse(payload).RootElement);

    private SubmitSynchronizationBatchCommand Batch(long sequence, params SynchronizationEvent[] events) =>
        new(Guid.NewGuid(), Guid.NewGuid(), _deviceId, sequence, Now.AddMinutes(-1), events);

    private SubmitSynchronizationBatchCommand CabinBatch(long sequence = 0) => Batch(sequence,
        Event(EdgeEventMessages.CabinWarningEventType, 0),
        Event(EdgeEventMessages.FatigueReadingEventType, 0),
        Event(EdgeEventMessages.FatigueReadingEventType, 1),
        Event(EdgeEventMessages.CabinWarningEventType, 2));

    [Fact]
    public async Task NewBatch_IsPublishedInCaptureOrderWithItsOriginalTimes()
    {
        // Arrange
        var command = CabinBatch();
        await _commandService.Handle(command);

        // Act
        var published = await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, published);
        Assert.Equal(command.Events.Select(item => item.EventId),
            _publisher.Published.Select(item => item.Event.GetPayload<EdgeEventSynchronized>().EdgeEventId));
        Assert.Equal(
        [
            IntegrationEventTopics.EdgeCabinWarnings, IntegrationEventTopics.EdgeFatigueReadings,
            IntegrationEventTopics.EdgeFatigueReadings, IntegrationEventTopics.EdgeCabinWarnings
        ], _publisher.Published.Select(item => item.Topic));
        Assert.Equal(command.Events.Select(item => item.OccurredAt), _publisher.Published.Select(item => item.Event.OccurredAt));
        Assert.All(_publisher.Published, item =>
        {
            var payload = item.Event.GetPayload<EdgeEventSynchronized>();
            Assert.Equal(item.Event.OccurredAt, payload.CapturedAt);
            Assert.Equal(Now, payload.ReceivedAt);
            Assert.Equal(_deviceId.ToString(), item.Key);
            Assert.Equal(command.BatchId, item.Event.CorrelationId);
        });
    }

    [Fact]
    public async Task PublishedEvent_CarriesTheCabinPayloadUntouched()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _commandService.Handle(Batch(0, Event(EdgeEventMessages.FatigueReadingEventType, 0,
            $$"""{"operatorId":"{{operatorId}}","perclos":0.31}""")));

        // Act
        await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        var data = Assert.Single(_publisher.Published).Event.Payload.GetProperty("data");
        Assert.Equal(operatorId, data.GetProperty("operatorId").GetGuid());
        Assert.Equal(0.31, data.GetProperty("perclos").GetDouble());
    }

    [Fact]
    public async Task DuplicateBatch_PublishesNothing()
    {
        // Arrange
        var command = CabinBatch();
        await _commandService.Handle(command);
        await _publication.PublishPendingAsync(CancellationToken.None);
        _publisher.Published.Clear();

        // Act
        var (_, isDuplicate) = await _commandService.Handle(command);
        var published = await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        Assert.True(isDuplicate);
        Assert.Equal(0, published);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task DuplicateWhilePending_PublishesTheBatchOnlyOnce()
    {
        // Arrange
        var command = CabinBatch();
        await _commandService.Handle(command);
        await _commandService.Handle(command);

        // Act
        await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal(command.Events.Count, _publisher.Published.Count);
    }

    [Fact]
    public async Task BrokerDown_KeepsTheBatchPendingAndRetriesWithTheSameEventIds()
    {
        // Arrange
        var command = CabinBatch();
        await _commandService.Handle(command);
        _publisher.FailAfter = 2;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _publication.PublishPendingAsync(CancellationToken.None));
        var stillPending = (await _commandService.Handle(command)).Batch.PublicationPending;
        var firstAttempt = _publisher.Published.Select(item => item.Event.EventId).ToList();
        _publisher.Published.Clear();
        _publisher.FailAfter = null;
        _time.Now = Now.AddMinutes(5);
        await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        Assert.True(stillPending);
        Assert.Equal(2, firstAttempt.Count);
        Assert.Equal(firstAttempt, _publisher.Published.Take(2).Select(item => item.Event.EventId));
        Assert.Equal(command.Events.Count, _publisher.Published.Count);
        var (published, _) = await _commandService.Handle(command);
        Assert.False(published.PublicationPending);
        Assert.Equal(Now.AddMinutes(5), published.PublishedAt);
    }

    [Fact]
    public async Task BrokerDown_ALaterBatchNeverOvertakesAnEarlierOne()
    {
        // Arrange
        var first = Batch(0, Event(EdgeEventMessages.FatigueReadingEventType, 0));
        var second = Batch(1, Event(EdgeEventMessages.FatigueReadingEventType, 1));
        await _commandService.Handle(first);
        _time.Now = Now.AddSeconds(1);
        await _commandService.Handle(second);
        _publisher.FailAfter = 0;

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _publication.PublishPendingAsync(CancellationToken.None));
        _publisher.FailAfter = null;
        await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        Assert.Equal([first.BatchId, second.BatchId],
            _publisher.Published.Select(item => item.Event.GetPayload<EdgeEventSynchronized>().BatchId));
    }

    [Fact]
    public async Task EventsThatCannotBePublished_StayStoredWithoutBlockingOrShiftingTheOthers()
    {
        // Arrange
        var known = Event(EdgeEventMessages.FatigueReadingEventType, 2);
        var command = Batch(0,
            Event("fatigue-assessment", 0),
            new SynchronizationEvent(Guid.NewGuid(), Captured, EdgeEventMessages.CabinWarningEventType, default),
            known);
        await _commandService.Handle(command);

        // Act
        await _publication.PublishPendingAsync(CancellationToken.None);

        // Assert
        var (_, integrationEvent, _) = Assert.Single(_publisher.Published);
        Assert.Equal(known.EventId, integrationEvent.GetPayload<EdgeEventSynchronized>().EdgeEventId);
        Assert.Equal(2, integrationEvent.GetPayload<EdgeEventSynchronized>().Position);
        Assert.Equal(EdgeEventMessages.EventId(command.BatchId, 2), integrationEvent.EventId);
        var (stored, _) = await _commandService.Handle(command);
        Assert.Equal(3, stored.Events.Count);
        Assert.False(stored.PublicationPending);
    }

    [Fact]
    public void EventId_IsDeterministicAndDependsOnBatchAndPosition()
    {
        // Arrange
        var batchId = Guid.NewGuid();

        // Act
        var id = EdgeEventMessages.EventId(batchId, 3);

        // Assert
        Assert.Equal(id, EdgeEventMessages.EventId(batchId, 3));
        Assert.NotEqual(id, EdgeEventMessages.EventId(batchId, 4));
        Assert.NotEqual(id, EdgeEventMessages.EventId(Guid.NewGuid(), 3));
        Assert.NotEqual(Guid.Empty, id);
    }
}
