using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Tests.Application;

public class ConsentServicesTests
{
    private static (PrivacyAuditCommandService commands, PrivacyAuditQueryService queries) CreateServices(
        FakeProcessingConsentRepository? repository = null)
    {
        // Arrange
        var repo = repository ?? new FakeProcessingConsentRepository();
        return (new PrivacyAuditCommandService(repo, null!, null!, TimeProvider.System),
            new PrivacyAuditQueryService(repo, new FakeAccessAuditRepository(),
                new FakeComplianceReportRepository()));
    }

    [Fact]
    public async Task RegisterConsent_NewDecision_ReturnsCreated()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v1.0",
            ["fatigue-monitoring", "safety-alerts"], ConsentStatus.Accepted);

        // Act
        var (consent, created) = await commands.Handle(command);

        // Assert
        Assert.True(created);
        Assert.NotEqual(Guid.Empty, consent.Id);
    }

    [Fact]
    public async Task RegisterConsent_SameDecisionTwice_ReturnsExistingWithoutDuplicate()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v1.0",
            ["fatigue-monitoring"], ConsentStatus.Declined);

        // Act
        var (first, firstCreated) = await commands.Handle(command);
        var (second, secondCreated) = await commands.Handle(command);
        var history = await queries.Handle(new GetConsentHistoryByOperatorIdQuery(command.OperatorId));

        // Assert
        Assert.True(firstCreated);
        Assert.False(secondCreated);
        Assert.Equal(first.Id, second.Id);
        Assert.Single(history);
    }

    [Fact]
    public async Task RegisterConsent_UnknownPolicyVersion_ThrowsDomainException()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v9.9",
            ["fatigue-monitoring"], ConsentStatus.Accepted);

        // Act
        Func<Task> act = () => commands.Handle(command);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task RegisterConsent_UnknownPurpose_ThrowsDomainException()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v1.0",
            ["mind-reading"], ConsentStatus.Accepted);

        // Act
        Func<Task> act = () => commands.Handle(command);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task GetCurrentConsent_ReturnsLatestDecision()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var operatorId = Guid.NewGuid();
        await commands.Handle(new RegisterConsentDecisionCommand(operatorId, "v1.0",
            ["fatigue-monitoring"], ConsentStatus.Declined));
        await Task.Delay(25); // ensure a later DecidedAt tick for the second decision
        await commands.Handle(new RegisterConsentDecisionCommand(operatorId, "v1.0",
            ["fatigue-monitoring", "safety-alerts"], ConsentStatus.Accepted));

        // Act
        var current = await queries.Handle(new GetCurrentConsentByOperatorIdQuery(operatorId));
        var history = await queries.Handle(new GetConsentHistoryByOperatorIdQuery(operatorId));

        // Assert
        Assert.NotNull(current);
        Assert.Equal(ConsentStatus.Accepted, current.Status);
        Assert.Equal(2, history.Count());
    }
}
