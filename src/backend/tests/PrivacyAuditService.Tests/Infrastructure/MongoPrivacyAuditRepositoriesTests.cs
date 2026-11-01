using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;
using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ReadModels;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Infrastructure.Persistence.MongoDb;

namespace PrivacyAuditService.Tests.Infrastructure;

public class MongoPrivacyAuditRepositoriesTests
{
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 30, 9, 0, 0, TimeSpan.Zero).AddTicks(1234);

    private static AccessAuditEntry Entry(Guid resourceId, string prevHash, int seconds) =>
        new(new RegisterAccessAttemptCommand(Guid.NewGuid(), "incident", resourceId, ["location", "biometrics"],
            "Incident follow-up", AccessDecision.Granted), Now.AddSeconds(seconds), prevHash);

    [Fact]
    public void Snapshot_RoundTrip_KeepsTheHashChainVerifiable()
    {
        // Arrange
        var first = Entry(Guid.NewGuid(), AccessAuditHashChain.GenesisPrevHash, 0);
        var second = Entry(Guid.NewGuid(), first.Hash, 1);

        // Act
        var restored = new[] { first, second }
            .Select(entry => AggregateSnapshot.Deserialize<AccessAuditEntry>(AggregateSnapshot.Serialize(entry)))
            .ToList();

        // Assert
        Assert.True(AccessAuditHashChain.Verify(restored));
        Assert.Equal(first.Fields, restored[0].Fields);
    }

    [MongoFact]
    public async Task Mongo_AccessLog_IsAppendOnlyLinearAndVerifiableAfterRestart()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("privacy_tests");
        var repository = new MongoAccessAuditRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var resourceId = Guid.NewGuid();
        var first = Entry(resourceId, AccessAuditHashChain.GenesisPrevHash, 0);
        var second = Entry(resourceId, first.Hash, 1);

        // Act
        await repository.AddAsync(first);
        await repository.AddAsync(second);
        var fork = await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(Entry(resourceId, first.Hash, 2)));
        var update = await Assert.ThrowsAsync<InvalidOperationException>(() => repository.UpdateAsync(first));
        var restarted = new MongoAccessAuditRepository(mongo.Reopen());
        var stored = (await restarted.FindByResourceAsync("incident", resourceId)).ToList();

        // Assert
        Assert.NotNull(fork);
        Assert.NotNull(update);
        Assert.Equal(new[] { first.Id, second.Id }, stored.Select(entry => entry.Id));
        Assert.True(AccessAuditHashChain.Verify(stored));
        Assert.Empty(await restarted.FindByResourceAsync("incident", Guid.NewGuid()));
    }

    [MongoFact]
    public async Task Mongo_ConsentsReportsAndLifecycle_SurviveRestart()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("privacy_tests");
        var consents = new MongoProcessingConsentRepository(mongo.Database);
        var reports = new MongoComplianceReportRepository(mongo.Database);
        var lifecycle = new MongoIncidentLifecycleRepository(mongo.Database);
        foreach (var repository in new IMongoCollectionInitializer[] { consents, reports, lifecycle })
            await repository.InitializeAsync(CancellationToken.None);
        var operatorId = Guid.NewGuid();
        var accepted = new ProcessingConsent(new RegisterConsentDecisionCommand(operatorId, "v1", ["safety"],
            ConsentStatus.Accepted), Now);
        var revoked = new ProcessingConsent(new RegisterConsentDecisionCommand(operatorId, "v1", ["safety"],
            ConsentStatus.Revoked), Now.AddDays(1));
        var report = new ComplianceReport(new RequestSafetyReportCommand(Now.AddDays(-30), Now, "site", null,
            Guid.NewGuid()), Now);
        var incidentId = Guid.NewGuid();
        var openedKey = Guid.NewGuid();
        var opened = new IncidentHistoryEntry(Now, "IncidentOpened", Guid.Empty, "Critical incident received.");
        var closed = new IncidentHistoryEntry(Now.AddMinutes(9), "IncidentClosed", Guid.NewGuid(), "Operator relieved.");

        // Act
        await consents.AddAsync(accepted);
        await consents.AddAsync(revoked);
        await reports.AddAsync(report);
        await lifecycle.AppendAsync(incidentId, Guid.NewGuid(), closed);
        await lifecycle.AppendAsync(incidentId, openedKey, opened);
        await lifecycle.AppendAsync(incidentId, openedKey, opened);
        var database = mongo.Reopen();

        // Assert
        Assert.Equal(new[] { revoked.Id, accepted.Id },
            (await new MongoProcessingConsentRepository(database).FindByOperatorIdAsync(operatorId)).Select(item => item.Id));
        Assert.Equal(report.From, (await new MongoComplianceReportRepository(database).FindByIdAsync(report.Id))!.From);
        Assert.Equal(new[] { opened, closed }, await new MongoIncidentLifecycleRepository(database).FindByIncidentIdAsync(incidentId));
    }
}
