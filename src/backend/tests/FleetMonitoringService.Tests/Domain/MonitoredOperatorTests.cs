using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Tests.Domain;

public class MonitoredOperatorTests
{
    private static readonly DateTimeOffset RegisteredAt = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static RegisterMonitoredOperatorCommand ValidCommand(Guid? operatorId = null, string fullName = "Juan Perez") =>
        new(operatorId ?? Guid.NewGuid(), fullName, "CAT-797-01", "Haulage", Shift.Night, "Pit North");

    [Fact]
    public void Constructor_WithValidCommand_StartsWithNormalRisk()
    {
        // Arrange
        var command = ValidCommand();

        // Act
        var monitoredOperator = new MonitoredOperator(command, RegisteredAt);

        // Assert
        Assert.Equal(RiskLevel.Normal, monitoredOperator.CurrentRiskLevel);
        Assert.Equal(RegisteredAt, monitoredOperator.LastUpdatedAt);
    }

    [Fact]
    public void Constructor_WithoutOperator_ThrowsDomainException()
    {
        // Arrange
        var command = ValidCommand(Guid.Empty);

        // Act & Assert
        Assert.Throws<DomainException>(() => new MonitoredOperator(command, RegisteredAt));
    }

    [Fact]
    public void Constructor_WithBlankFullName_ThrowsDomainException()
    {
        // Arrange
        var command = ValidCommand(fullName: "  ");

        // Act & Assert
        Assert.Throws<DomainException>(() => new MonitoredOperator(command, RegisteredAt));
    }

    [Fact]
    public void UpdateRiskLevel_RecordsLevelAndTimestamp()
    {
        // Arrange
        var monitoredOperator = new MonitoredOperator(ValidCommand(), RegisteredAt);
        var updatedAt = RegisteredAt.AddMinutes(5);

        // Act
        monitoredOperator.UpdateRiskLevel(RiskLevel.Critical, updatedAt);

        // Assert
        Assert.Equal(RiskLevel.Critical, monitoredOperator.CurrentRiskLevel);
        Assert.Equal(updatedAt, monitoredOperator.LastUpdatedAt);
    }

    [Fact]
    public void ReassignVehicle_WithBlankVehicleCode_ThrowsDomainException()
    {
        // Arrange
        var monitoredOperator = new MonitoredOperator(ValidCommand(), RegisteredAt);

        // Act & Assert
        Assert.Throws<DomainException>(() => monitoredOperator.ReassignVehicle("", "Pit South"));
    }
}
