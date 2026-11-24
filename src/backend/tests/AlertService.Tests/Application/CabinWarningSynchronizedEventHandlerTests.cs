using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.EventHandlers;
using AlertService.Application.Internal.IntegrationEvents;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Messaging;

namespace AlertService.Tests.Application;

public class CabinWarningSynchronizedEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset WarnedAt = Now.AddHours(-3);

    private readonly FakeAlertRepository _repository = new();
    private readonly FakeIntegrationEventPublisher _publisher = new();
    private readonly AlertCommandService _commandService;
    private readonly CabinWarningSynchronizedEventHandler _handler;
    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly Guid _readingId = Guid.NewGuid();
    private readonly Guid _warningId = Guid.NewGuid();
    private readonly Guid _batchId = Guid.NewGuid();

    public CabinWarningSynchronizedEventHandlerTests()
    {
        _commandService = new AlertCommandService(_repository, new FakeTimeProvider(Now), _publisher);
        _handler = new CabinWarningSynchronizedEventHandler(_commandService,
            NullLogger<CabinWarningSynchronizedEventHandler>.Instance);
    }

    private IntegrationEvent Warning(string severity = "Critical", Guid? readingEventId = null, Guid? eventId = null) =>
        IntegrationEvent.Create("CabinWarningSynchronized", "edge-synchronization-service", _batchId, new
        {
            edgeEventId = _warningId,
            capturedAt = WarnedAt,
            data = new
            {
                operatorId = _operatorId,
                severity,
                message = "Posible microsueño.",
                deliveredChannels = new[] { "display", "buzzer" },
                readingEventId = readingEventId ?? _readingId
            }
        }, WarnedAt, correlationId: _batchId, eventId: eventId);

    [Fact]
    public async Task HandleAsync_RecordsTheWarningWithItsOriginalTimeAndOutOfTheAcknowledgementFlow()
    {
        // Act
        await _handler.HandleAsync(Warning(), CancellationToken.None);

        // Assert
        var alert = Assert.Single(_repository.Items);
        Assert.Equal(_operatorId, alert.OperatorId);
        Assert.Equal(_readingId, alert.AssessmentId);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        Assert.Equal(AlertStatus.RecordedFromEdge, alert.Status);
        Assert.Equal(AlertOrigin.Edge, alert.Origin);
        Assert.Equal(WarnedAt, alert.IssuedAt);
        Assert.Equal(Now, alert.SyncedAt);
    }

    [Fact]
    public async Task HandleAsync_PublishesAlertIssuedWithTheOriginalTimeForTheDownstreamContexts()
    {
        // Act
        using (IntegrationEventContext.Begin(_batchId))
            await _handler.HandleAsync(Warning(), CancellationToken.None);

        // Assert
        var (topic, published, _) = Assert.Single(_publisher.Published);
        Assert.Equal(IntegrationEventTopics.AlertIssued, topic);
        Assert.Equal(WarnedAt, published.OccurredAt);
        Assert.Equal(_batchId, published.CorrelationId);
        var payload = published.GetPayload<AlertIssuedEvent>();
        Assert.Equal(WarnedAt, payload.IssuedAt);
        Assert.Equal("Edge", payload.Origin);
        Assert.Equal("Critical", payload.Severity);
    }

    [Fact]
    public async Task HandleAsync_RedeliveryWithAnotherEventId_DoesNotDuplicateTheAlertNorItsEvent()
    {
        // Arrange: the processed-event store forgot the event, so only the business key can catch it
        await _handler.HandleAsync(Warning(), CancellationToken.None);

        // Act
        await _handler.HandleAsync(Warning(), CancellationToken.None);

        // Assert
        Assert.Single(_repository.Items);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Processor_SameEventIdTwice_RecordsTheWarningOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationEventHandler>(_handler);
        var processor = new IntegrationEventProcessor(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new InMemoryProcessedEventStore(), NullLogger<IntegrationEventProcessor>.Instance, 1, TimeSpan.Zero);
        var integrationEvent = Warning(eventId: Guid.NewGuid());

        // Act
        var first = await processor.ProcessAsync(IntegrationEventTopics.EdgeCabinWarnings, integrationEvent, CancellationToken.None);
        var second = await processor.ProcessAsync(IntegrationEventTopics.EdgeCabinWarnings, integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Handled, first);
        Assert.Equal(IntegrationEventOutcome.Duplicate, second);
        Assert.Single(_repository.Items);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task FatigueAssessedForTheSameReading_DoesNotIssueASecondWarning()
    {
        // Arrange
        await _handler.HandleAsync(Warning(), CancellationToken.None);

        // Act
        var assessed = new FatigueAssessedEventHandler(_commandService, NullLogger<FatigueAssessedEventHandler>.Instance);
        await assessed.HandleAsync(IntegrationEvent.Create("FatigueAssessed", "fatigue-detection-service", _readingId,
            new { assessmentId = _readingId, operatorId = _operatorId, riskLevel = "Critical", requiresAlert = true }, Now),
            CancellationToken.None);

        // Assert
        var alert = Assert.Single(_repository.Items);
        Assert.Equal(AlertStatus.RecordedFromEdge, alert.Status);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task OverdueEscalation_IgnoresARecordedWarningHoursOld()
    {
        // Arrange: 3 hours exceed any acknowledgement deadline
        await _handler.HandleAsync(Warning(), CancellationToken.None);
        _publisher.Published.Clear();

        // Act
        var escalated = await _commandService.Handle(new EscalateOverdueAlertsCommand());

        // Assert
        Assert.Empty(escalated);
        Assert.Empty(_publisher.Published);
        Assert.False(_repository.Items[0].IsAcknowledgementOverdue(Now));
    }

    [Fact]
    public async Task RecordedWarning_CannotBeAcknowledgedOrEscalated()
    {
        // Arrange
        await _handler.HandleAsync(Warning(), CancellationToken.None);
        var alertId = _repository.Items[0].Id;

        // Act and assert
        await Assert.ThrowsAsync<DomainException>(() => _commandService.Handle(new AcknowledgeAlertCommand(alertId)));
        await Assert.ThrowsAsync<DomainException>(() => _commandService.Handle(new EscalateAlertCommand(alertId)));
    }

    [Fact]
    public async Task HandleAsync_WarningWithoutReadingId_FallsBackToItsOwnEventId()
    {
        // Arrange
        var integrationEvent = IntegrationEvent.Create("CabinWarningSynchronized", "edge-synchronization-service", _batchId,
            new { edgeEventId = _warningId, capturedAt = WarnedAt, data = new { operatorId = _operatorId, severity = "Warning" } },
            WarnedAt);

        // Act
        await _handler.HandleAsync(integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(_warningId, Assert.Single(_repository.Items).AssessmentId);
    }

    [Fact]
    public async Task HandleAsync_UnknownSeverity_IsNotRecorded()
    {
        // Act
        await _handler.HandleAsync(Warning(severity: "Normal"), CancellationToken.None);

        // Assert
        Assert.Empty(_repository.Items);
        Assert.Empty(_publisher.Published);
    }
}
