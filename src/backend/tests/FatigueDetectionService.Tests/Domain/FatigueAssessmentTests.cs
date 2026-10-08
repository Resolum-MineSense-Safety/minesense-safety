using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FatigueDetectionService.Tests.Domain;

public class FatigueAssessmentTests
{
    private static readonly DateTimeOffset AssessedAt = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(0.10, RiskLevel.Normal, false)]
    [InlineData(0.15, RiskLevel.Warning, true)]
    [InlineData(0.40, RiskLevel.Critical, true)]
    public void RequiresAlert_WithClassifiedRiskLevel_ReturnsExpectedResult(
        double perclos, RiskLevel expectedRiskLevel, bool expectedRequiresAlert)
    {
        // Arrange
        var command = new AssessFatigueCommand(Guid.NewGuid(), Guid.NewGuid(), perclos, 15, 50);
        var assessment = new FatigueAssessment(command, AssessedAt);

        // Act
        var requiresAlert = assessment.RequiresAlert;

        // Assert
        Assert.Equal(expectedRiskLevel, assessment.RiskLevel);
        Assert.Equal(expectedRequiresAlert, requiresAlert);
    }

    [Fact]
    public void Constructor_WithValidCommand_ClassifiesRiskLevel()
    {
        // Arrange
        var command = new AssessFatigueCommand(Guid.NewGuid(), Guid.NewGuid(), 0.45, 20, 40);

        // Act
        var assessment = new FatigueAssessment(command, AssessedAt);

        // Assert
        Assert.Equal(RiskLevel.Critical, assessment.RiskLevel);
        Assert.Equal(AssessedAt, assessment.AssessedAt);
    }

    [Fact]
    public void Constructor_WithoutOperator_ThrowsDomainException()
    {
        // Arrange
        var command = new AssessFatigueCommand(Guid.Empty, Guid.NewGuid(), 0.10, 15, 50);

        // Act & Assert
        Assert.Throws<DomainException>(() => new FatigueAssessment(command, AssessedAt));
    }

    [Theory]
    [InlineData(1.2, 15, 50)]
    [InlineData(0.10, -1, 50)]
    [InlineData(0.10, 15, 0)]
    public void BiometricSignals_WithInvalidValues_ThrowsDomainException(
        double perclos, double blinkRate, double heartRateVariability)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new BiometricSignals(perclos, blinkRate, heartRateVariability));
    }
}
