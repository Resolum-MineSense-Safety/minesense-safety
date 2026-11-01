using FatigueDetectionService.Application.Internal.CommandServices;
using FatigueDetectionService.Application.Internal.EventHandlers;
using FatigueDetectionService.Application.Internal.QueryServices;
using FatigueDetectionService.Domain.Model.Queries;
using FatigueDetectionService.Domain.Model.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;

namespace FatigueDetectionService.Tests.Application;

public class FatigueReadingSynchronizedEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Captured = Now.AddHours(-3);

    private readonly FakeFatigueAssessmentRepository _repository = new();
    private readonly FakeIntegrationEventPublisher _publisher = new();
    private readonly FatigueReadingSynchronizedEventHandler _handler;
    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly Guid _readingId = Guid.NewGuid();

    public FatigueReadingSynchronizedEventHandlerTests()
    {
        var commandService = new FatigueAssessmentCommandService(_repository, new FixedTimeProvider(Now), _publisher);
        _handler = new FatigueReadingSynchronizedEventHandler(commandService,
            NullLogger<FatigueReadingSynchronizedEventHandler>.Instance);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private IntegrationEvent Reading(double perclos = 0.46, double hrv = 40, Guid? eventId = null) =>
        IntegrationEvent.Create("FatigueReadingSynchronized", "edge-synchronization-service", Guid.NewGuid(), new
        {
            edgeEventId = _readingId,
            capturedAt = Captured,
            data = new
            {
                operatorId = _operatorId,
                monitoringSessionId = Guid.NewGuid(),
                perclos,
                blinkRatePerMinute = 12.0,
                heartRateVariabilityMs = hrv,
                riskLevel = "Critical",
                classifiedOffline = true
            }
        }, Captured, eventId: eventId);

    [Fact]
    public async Task HandleAsync_KeepsTheCaptureTimeAndFlagsTheAssessmentAsEdge()
    {
        // Act
        await _handler.HandleAsync(Reading(), CancellationToken.None);

        // Assert
        var assessment = Assert.Single(_repository.Items);
        Assert.Equal(_readingId, assessment.Id);
        Assert.Equal(_operatorId, assessment.OperatorId);
        Assert.Equal(Captured, assessment.AssessedAt);
        Assert.Equal(AssessmentOrigin.Edge, assessment.Origin);
        Assert.Equal(Now, assessment.SyncedAt);
        Assert.Equal(RiskLevel.Critical, assessment.RiskLevel);
    }

    [Fact]
    public async Task HandleAsync_PublishesNoFatigueAssessed_SoNobodyWarnsOrChangesTheCurrentRiskAgain()
    {
        // Act
        await _handler.HandleAsync(Reading(), CancellationToken.None);

        // Assert
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task HandleAsync_RedeliveryWithAnotherEventId_DoesNotDuplicateTheAssessment()
    {
        // Arrange: the processed-event store forgot the event, so only the business key can catch it
        await _handler.HandleAsync(Reading(), CancellationToken.None);

        // Act
        await _handler.HandleAsync(Reading(), CancellationToken.None);

        // Assert
        Assert.Single(_repository.Items);
    }

    [Fact]
    public async Task Processor_SameEventIdTwice_RunsTheHandlerOnce()
    {
        // Arrange
        var services = new ServiceCollection();
        var counting = new CountingHandler(_handler);
        services.AddSingleton<IIntegrationEventHandler>(counting);
        var processor = new IntegrationEventProcessor(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            new InMemoryProcessedEventStore(), NullLogger<IntegrationEventProcessor>.Instance, 1, TimeSpan.Zero);
        var integrationEvent = Reading(eventId: Guid.NewGuid());

        // Act
        var first = await processor.ProcessAsync(IntegrationEventTopics.EdgeFatigueReadings, integrationEvent, CancellationToken.None);
        var second = await processor.ProcessAsync(IntegrationEventTopics.EdgeFatigueReadings, integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Handled, first);
        Assert.Equal(IntegrationEventOutcome.Duplicate, second);
        Assert.Equal(1, counting.Calls);
        Assert.Single(_repository.Items);
    }

    private sealed class CountingHandler(IIntegrationEventHandler inner) : IIntegrationEventHandler
    {
        public int Calls { get; private set; }
        public string Topic => inner.Topic;

        public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Calls++;
            return inner.HandleAsync(integrationEvent, cancellationToken);
        }
    }

    [Fact]
    public async Task HandleAsync_InvalidSignals_AreDroppedWithoutFailing()
    {
        // Act
        await _handler.HandleAsync(Reading(perclos: 7), CancellationToken.None);

        // Assert
        Assert.Empty(_repository.Items);
    }

    [Fact]
    public async Task History_PlacesAnOldReadingBehindANewerAssessment()
    {
        // Arrange
        var commandService = new FatigueAssessmentCommandService(_repository, new FixedTimeProvider(Now), _publisher);
        var live = await commandService.Handle(new FatigueDetectionService.Domain.Model.Commands.AssessFatigueCommand(
            _operatorId, Guid.NewGuid(), 0.05, 12, 40));
        await _handler.HandleAsync(Reading(), CancellationToken.None);

        // Act
        var latest = await new FatigueAssessmentQueryService(_repository)
            .Handle(new GetLatestFatigueAssessmentByOperatorIdQuery(_operatorId));

        // Assert: the reading that arrived last does not become the current state
        Assert.Equal(live.Id, latest!.Id);
        Assert.Equal(2, _repository.Items.Count);
    }
}
