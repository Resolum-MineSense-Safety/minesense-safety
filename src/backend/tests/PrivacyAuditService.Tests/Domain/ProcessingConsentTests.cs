using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Tests.Domain;

public class ProcessingConsentTests
{
    private static RegisterConsentDecisionCommand AcceptedCommand(Guid? operatorId = null) =>
        new(operatorId ?? Guid.NewGuid(), "v1.0",
            ["fatigue-monitoring", "safety-alerts"], ConsentStatus.Accepted);

    [Fact]
    public void Constructor_WithAcceptedFullPurposes_DoesNotBlockMonitoring()
    {
        // Arrange
        var command = AcceptedCommand();

        // Act
        var consent = new ProcessingConsent(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(ConsentStatus.Accepted, consent.Status);
        Assert.False(ConsentPolicy.BlocksMonitoring(consent));
    }

    [Fact]
    public void Constructor_WithEmptyOperatorId_ThrowsDomainException()
    {
        // Arrange
        var command = AcceptedCommand(Guid.Empty);

        // Act
        Action act = () => new ProcessingConsent(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Theory]
    [InlineData(ConsentStatus.Declined)]
    [InlineData(ConsentStatus.Revoked)]
    public void BlocksMonitoring_WhenNotAccepted_ReturnsTrue(ConsentStatus status)
    {
        // Arrange
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v1.0",
            ["fatigue-monitoring", "safety-alerts"], status);

        // Act
        var consent = new ProcessingConsent(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(ConsentPolicy.BlocksMonitoring(consent));
    }

    [Fact]
    public void BlocksMonitoring_WhenMandatoryPurposeMissing_ReturnsTrue()
    {
        // Arrange: accepted but without the mandatory safety-alerts purpose
        var command = new RegisterConsentDecisionCommand(Guid.NewGuid(), "v1.0",
            ["fatigue-monitoring", "operational-analytics"], ConsentStatus.Accepted);

        // Act
        var consent = new ProcessingConsent(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(ConsentPolicy.BlocksMonitoring(consent));
    }
}
