using IncidentManagementService.Application.Internal.CommandServices;
using IncidentManagementService.Application.Internal.EventHandlers;
using IncidentManagementService.Application.Internal.IntegrationEvents;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace IncidentManagementService.Tests.Application;

public class AlertIssuedEventHandlerTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 30, 8, 0, 0, TimeSpan.Zero);
    private readonly FakeIncidentRepository _repository = new();
    private readonly FakeIntegrationEventPublisher _publisher = new();
    private readonly IncidentCommandService _commandService;
    private readonly AlertIssuedEventHandler _handler;

    public AlertIssuedEventHandlerTests()
    {
        _commandService = new IncidentCommandService(_repository, new FakeTimeProvider(Start), _publisher);
        _handler = new AlertIssuedEventHandler(_commandService, NullLogger<AlertIssuedEventHandler>.Instance);
    }

    private static IntegrationEvent Issued(Guid alertId, string severity) =>
        IntegrationEvent.Create("AlertIssued", "alert-service", alertId,
            new { alertId, operatorId = Guid.NewGuid(), assessmentId = Guid.NewGuid(), severity, issuedAt = Start },
            Start);

    [Fact]
    public async Task HandleAsync_CriticalAlert_OpensIncidentAndPublishesIncidentOpened()
    {
        // Arrange
        var alertId = Guid.NewGuid();

        // Act
        await _handler.HandleAsync(Issued(alertId, "Critical"), CancellationToken.None);

        // Assert
        var incidents = (await _repository.FindAsync(null, null)).ToList();
        var incident = Assert.Single(incidents);
        Assert.Equal(alertId, incident.AlertId);
        Assert.Equal(IncidentSeverity.Critical, incident.Severity);
        var (topic, published, _) = Assert.Single(_publisher.Published);
        Assert.Equal(IntegrationEventTopics.IncidentOpened, topic);
        Assert.Equal(incident.Id, published.GetPayload<IncidentOpenedEvent>().IncidentId);
    }

    [Fact]
    public async Task HandleAsync_WarningAlert_OpensNoIncident()
    {
        // Act
        await _handler.HandleAsync(Issued(Guid.NewGuid(), "Warning"), CancellationToken.None);

        // Assert
        Assert.Empty(await _repository.FindAsync(null, null));
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task HandleAsync_RedeliveredCriticalAlert_KeepsOneIncidentAndOneEvent()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        await _handler.HandleAsync(Issued(alertId, "Critical"), CancellationToken.None);

        // Act
        await _handler.HandleAsync(Issued(alertId, "Critical"), CancellationToken.None);

        // Assert
        Assert.Single(await _repository.FindAsync(null, null));
        Assert.Single(_publisher.Published);
    }

    [Fact]
    public async Task Handle_CloseIncident_PublishesIncidentClosed()
    {
        // Arrange
        var supervisor = Guid.NewGuid();
        var incident = await _commandService.Handle(new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid(), IncidentSeverity.Critical));
        await _commandService.Handle(new AssignIncidentCommand(incident.Id, supervisor));
        await _commandService.Handle(new RecordIncidentActionCommand(incident.Id, IncidentActionType.PreventivePause,
            "Pause", "Rested", supervisor, Start.AddMinutes(1)));

        // Act
        await _commandService.Handle(new CloseIncidentCommand(incident.Id, "Operator relieved", Start.AddMinutes(5)));

        // Assert
        var (topic, published, _) = _publisher.Published[^1];
        Assert.Equal(IntegrationEventTopics.IncidentClosed, topic);
        var closed = published.GetPayload<IncidentClosedEvent>();
        Assert.Equal(supervisor, closed.ClosedBy);
        Assert.Equal("Operator relieved", closed.Resolution);
        Assert.Equal(1, closed.ActionCount);
    }
}
