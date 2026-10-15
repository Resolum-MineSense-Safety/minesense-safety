using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Tests.Application;

public class IncidentHistoryServicesTests
{
    private static (PrivacyAuditCommandService commands, PrivacyAuditQueryService queries) CreateServices()
    {
        // Arrange
        var audits = new FakeAccessAuditRepository();
        var consents = new FakeProcessingConsentRepository();
        var reports = new FakeComplianceReportRepository();
        return (new PrivacyAuditCommandService(consents, audits, reports, TimeProvider.System),
            new PrivacyAuditQueryService(consents, audits, reports));
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
}
