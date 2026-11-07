using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Tests.Application;

public class AccessAuditServicesTests
{
    private static (PrivacyAuditCommandService commands, PrivacyAuditQueryService queries) CreateServices()
    {
        // Arrange
        var audits = new FakeAccessAuditRepository();
        return (new PrivacyAuditCommandService(new FakeProcessingConsentRepository(), audits,
                new FakeComplianceReportRepository(), TimeProvider.System),
            new PrivacyAuditQueryService(new FakeProcessingConsentRepository(), audits,
                new FakeComplianceReportRepository()));
    }

    private static RegisterAccessAttemptCommand AttemptCommand(
        Guid? userId = null, AccessDecision decision = AccessDecision.Granted) =>
        new(userId ?? Guid.NewGuid(), "FatigueAssessment", Guid.NewGuid(),
            ["riskLevel"], "shift-supervision", decision);

    [Fact]
    public async Task RegisterAttempt_LinksHashChain()
    {
        // Arrange
        var (commands, _) = CreateServices();

        // Act
        var first = await commands.Handle(AttemptCommand());
        var second = await commands.Handle(AttemptCommand());

        // Assert
        Assert.Equal(first.Hash, second.PrevHash);
        Assert.True(AccessAuditHashChain.Verify([first, second]));
    }

    [Fact]
    public async Task RegisterAttempt_Denied_IsRecorded()
    {
        // Arrange
        var (commands, _) = CreateServices();

        // Act
        var entry = await commands.Handle(AttemptCommand(decision: AccessDecision.Denied));

        // Assert
        Assert.Equal(AccessDecision.Denied, entry.Decision);
    }

    [Fact]
    public async Task RegisterAttempt_EmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = AttemptCommand(userId: Guid.Empty);

        // Act
        Func<Task> act = () => commands.Handle(command);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task GetAccessLog_FiltersByDecisionAndPaginates()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        await commands.Handle(AttemptCommand(decision: AccessDecision.Granted));
        await commands.Handle(AttemptCommand(decision: AccessDecision.Denied));
        await commands.Handle(AttemptCommand(decision: AccessDecision.Denied));

        // Act
        var denied = await queries.Handle(new GetAccessLogQuery(null, null, null, null, null,
            AccessDecision.Denied, 1, 20));
        var firstPage = await queries.Handle(new GetAccessLogQuery(null, null, null, null, null,
            null, 1, 2));
        var secondPage = await queries.Handle(new GetAccessLogQuery(null, null, null, null, null,
            null, 2, 2));

        // Assert
        Assert.Equal(2, denied.Count());
        Assert.Equal(2, firstPage.Count());
        Assert.Single(secondPage);
    }
}
