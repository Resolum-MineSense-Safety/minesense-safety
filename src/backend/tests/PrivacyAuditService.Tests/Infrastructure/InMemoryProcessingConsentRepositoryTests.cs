using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Infrastructure.Persistence.InMemory;

namespace PrivacyAuditService.Tests.Infrastructure;

public class InMemoryProcessingConsentRepositoryTests
{
    [Fact]
    public async Task FindByOperatorIdAsync_ReturnsNewestFirst()
    {
        // Arrange
        var repository = new InMemoryProcessingConsentRepository();
        var operatorId = Guid.NewGuid();
        await repository.AddAsync(new ProcessingConsent(
            new RegisterConsentDecisionCommand(operatorId, "v1.0", ["fatigue-monitoring"], ConsentStatus.Declined),
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)));
        await repository.AddAsync(new ProcessingConsent(
            new RegisterConsentDecisionCommand(operatorId, "v1.0",
                ["fatigue-monitoring", "safety-alerts"], ConsentStatus.Accepted),
            new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero)));

        // Act
        var result = (await repository.FindByOperatorIdAsync(operatorId)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(ConsentStatus.Accepted, result[0].Status);
    }
}
