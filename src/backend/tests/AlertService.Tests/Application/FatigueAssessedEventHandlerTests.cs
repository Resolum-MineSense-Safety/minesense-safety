using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.EventHandlers;
using AlertService.Application.Internal.IntegrationEvents;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AlertService.Tests.Application;

public class FatigueAssessedEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 8, 0, 0, TimeSpan.Zero);
    private readonly FakeAlertRepository _repository = new();
    private readonly FakeIntegrationEventPublisher _publisher = new();
    private readonly AlertCommandService _commandService;
    private readonly FatigueAssessedEventHandler _handler;

    public FatigueAssessedEventHandlerTests()
    {
        _commandService = new AlertCommandService(_repository, new FakeTimeProvider(Now), _publisher);
        _handler = new FatigueAssessedEventHandler(_commandService, NullLogger<FatigueAssessedEventHandler>.Instance);
    }

    private static IntegrationEvent Assessed(Guid assessmentId, string riskLevel, bool requiresAlert) =>
        IntegrationEvent.Create("FatigueAssessed", "fatigue-detection-service", assessmentId,
            new { assessmentId, operatorId = Guid.NewGuid(), riskLevel, requiresAlert, perclos = 0.46 }, Now,
            correlationId: assessmentId);

    [Fact]
    public async Task HandleAsync_AssessmentRequiringAlert_IssuesAlertWithItsSeverity()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();

        // Act
        await _handler.HandleAsync(Assessed(assessmentId, "Critical", requiresAlert: true), CancellationToken.None);

        // Assert
        var alert = Assert.Single(_repository.Items);
        Assert.Equal(assessmentId, alert.AssessmentId);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
        var (topic, published, _) = Assert.Single(_publisher.Published);
        Assert.Equal(IntegrationEventTopics.AlertIssued, topic);
        Assert.Equal(assessmentId, published.CorrelationId);
        Assert.Equal(alert.Id, published.GetPayload<AlertIssuedEvent>().AlertId);
    }

    [Fact]
    public async Task HandleAsync_NormalAssessment_IssuesNoAlert()
    {
        // Act
        await _handler.HandleAsync(Assessed(Guid.NewGuid(), "Normal", requiresAlert: false), CancellationToken.None);

        // Assert
        Assert.Empty(_repository.Items);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task HandleAsync_RedeliveredAssessment_DoesNotDuplicateTheAlert()
    {
        // Arrange
        var assessmentId = Guid.NewGuid();
        await _handler.HandleAsync(Assessed(assessmentId, "Warning", requiresAlert: true), CancellationToken.None);

        // Act: same assessment again, as a new delivery with another event id
        await _handler.HandleAsync(Assessed(assessmentId, "Warning", requiresAlert: true), CancellationToken.None);

        // Assert
        Assert.Single(_repository.Items);
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Handle_IssueAlertForAlreadyAlertedAssessment_ReturnsExistingAlert()
    {
        // Arrange
        var command = new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Critical);
        var first = await _commandService.Handle(command);

        // Act
        var second = await _commandService.Handle(command);

        // Assert
        Assert.Same(first, second);
        Assert.Single(_repository.Items);
    }

    [Fact]
    public async Task Handle_AcknowledgeAndEscalate_PublishTheirEvents()
    {
        // Arrange
        var toAcknowledge = await _commandService.Handle(new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Warning));
        var toEscalate = await _commandService.Handle(new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Critical));

        // Act
        await _commandService.Handle(new AcknowledgeAlertCommand(toAcknowledge.Id));
        await _commandService.Handle(new EscalateAlertCommand(toEscalate.Id));

        // Assert
        Assert.Equal(
            [IntegrationEventTopics.AlertIssued, IntegrationEventTopics.AlertIssued,
                IntegrationEventTopics.AlertAcknowledged, IntegrationEventTopics.AlertEscalated],
            _publisher.Published.Select(item => item.Topic));
        Assert.Equal(toEscalate.Id, _publisher.Published[^1].Event.GetPayload<AlertEscalatedEvent>().AlertId);
    }
}
