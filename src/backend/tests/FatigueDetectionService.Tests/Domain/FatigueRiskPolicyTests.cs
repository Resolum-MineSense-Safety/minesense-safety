using FatigueDetectionService.Domain.Model.ValueObjects;
using FatigueDetectionService.Domain.Services;

namespace FatigueDetectionService.Tests.Domain;

public class FatigueRiskPolicyTests
{
    [Fact]
    public void Classify_RestedSignals_ReturnsNormal()
    {
        // Arrange
        var signals = new BiometricSignals(0.10, 15, 50);

        // Act
        var riskLevel = FatigueRiskPolicy.Classify(signals);

        // Assert
        Assert.Equal(RiskLevel.Normal, riskLevel);
    }

    [Theory]
    [InlineData(0.15, 15, 50)]
    [InlineData(0.10, 25, 50)]
    [InlineData(0.10, 15, 29)]
    public void Classify_SignalsAtWarningThreshold_ReturnsWarning(
        double perclos, double blinkRate, double heartRateVariability)
    {
        // Arrange
        var signals = new BiometricSignals(perclos, blinkRate, heartRateVariability);

        // Act
        var riskLevel = FatigueRiskPolicy.Classify(signals);

        // Assert
        Assert.Equal(RiskLevel.Warning, riskLevel);
    }

    [Fact]
    public void Classify_PerclosAtCriticalThreshold_ReturnsCritical()
    {
        // Arrange
        var signals = new BiometricSignals(0.40, 15, 50);

        // Act
        var riskLevel = FatigueRiskPolicy.Classify(signals);

        // Assert
        Assert.Equal(RiskLevel.Critical, riskLevel);
    }

    [Fact]
    public void Classify_ModeratePerclosWithLowHeartRateVariability_ReturnsCritical()
    {
        // Arrange
        var signals = new BiometricSignals(0.25, 15, 19);

        // Act
        var riskLevel = FatigueRiskPolicy.Classify(signals);

        // Assert
        Assert.Equal(RiskLevel.Critical, riskLevel);
    }
}
