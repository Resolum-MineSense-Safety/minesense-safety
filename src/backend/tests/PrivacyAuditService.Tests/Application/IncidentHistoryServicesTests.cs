using MineSenseSafety.Shared.Application.IntegrationEvents;
using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.EventHandlers;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Infrastructure.Persistence.InMemory;

namespace PrivacyAuditService.Tests.Application;

public class IncidentHistoryServicesTests
{
    private readonly InMemoryIncidentLifecycleRepository _lifecycle = new();

    private (PrivacyAuditCommandService commands, PrivacyAuditQueryService queries) CreateServices()
    {
        // Arrange
        var audits = new FakeAccessAuditRepository();
        var consents = new FakeProcessingConsentRepository();
        var reports = new FakeComplianceReportRepository();
        return (new PrivacyAuditCommandService(consents, audits, reports, TimeProvider.System),
            new PrivacyAuditQueryService(consents, audits, reports, _lifecycle));
    }

    private static RegisterAccessAttemptCommand AttemptOn(Guid resourceId) =>
        new(Guid.NewGuid(), "Incident", resourceId, ["status"], "incident-review", AccessDecision.Granted);

    [Fact]
    public async Task GetIncidentHistory_ReturnsChronologicalSteps()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var incidentId = Guid.NewGuid();
        await commands.Handle(AttemptOn(incidentId));
        await Task.Delay(25); // ensure a later AccessedAt tick for the second step
        await commands.Handle(AttemptOn(incidentId));
        await commands.Handle(AttemptOn(Guid.NewGuid())); // other incident, must not leak

        // Act
        var history = (await queries.Handle(new GetIncidentHistoryQuery(incidentId))).ToList();

        // Assert
        Assert.Equal(2, history.Count);
        Assert.True(history[0].OccurredAt <= history[1].OccurredAt);
        Assert.All(history, step => Assert.Equal("AccessGranted", step.Kind));
    }

    [Fact]
    public async Task GetIncidentHistory_WithoutSteps_ReturnsEmpty()
    {
        // Arrange
        var (_, queries) = CreateServices();

        // Act
        var history = await queries.Handle(new GetIncidentHistoryQuery(Guid.NewGuid()));

        // Assert
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetIncidentHistory_IncludesOpeningAndClosureEventsInOrder()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var incidentId = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var openedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var opened = IntegrationEvent.Create("IncidentOpened", "incident-management-service", incidentId,
            new { incidentId, operatorId = Guid.NewGuid(), alertId = Guid.NewGuid(), severity = "Critical", openedAt },
            openedAt);
        var closed = IntegrationEvent.Create("IncidentClosed", "incident-management-service", incidentId,
            new { incidentId, closedBy = supervisor, resolution = "Operator relieved", actionCount = 2,
                closedAt = DateTimeOffset.UtcNow.AddMinutes(10) }, DateTimeOffset.UtcNow.AddMinutes(10));
        await commands.Handle(AttemptOn(incidentId));

        // Act
        await new IncidentClosedEventHandler(_lifecycle).HandleAsync(closed, CancellationToken.None);
        await new IncidentOpenedEventHandler(_lifecycle).HandleAsync(opened, CancellationToken.None);
        await new IncidentOpenedEventHandler(_lifecycle).HandleAsync(opened, CancellationToken.None); // redelivery
        var history = (await queries.Handle(new GetIncidentHistoryQuery(incidentId))).ToList();

        // Assert
        Assert.Equal(["IncidentOpened", "AccessGranted", "IncidentClosed"], history.Select(step => step.Kind));
        Assert.Equal(supervisor, history[^1].Actor);
        Assert.DoesNotContain("Operator relieved", history[^1].Summary);
    }
}
