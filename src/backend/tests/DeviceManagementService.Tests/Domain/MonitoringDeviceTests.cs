using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Domain;

public class MonitoringDeviceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);

    private static MonitoringDevice RegisterWearable() =>
        new(new RegisterDeviceCommand("WR-0001", DeviceType.Wearable));

    [Fact]
    public void Constructor_WithValidCommand_StartsAvailableAndUnassigned()
    {
        // Act
        var device = new MonitoringDevice(new RegisterDeviceCommand("  CAM-01 ", DeviceType.Camera));

        // Assert
        Assert.Equal("CAM-01", device.SerialNumber);
        Assert.Equal(DeviceType.Camera, device.Type);
        Assert.Equal(DeviceStatus.Available, device.Status);
        Assert.Null(device.AssignedOperatorId);
        Assert.Null(device.BatteryLevel);
        Assert.Null(device.LastSeenAt);
        Assert.Empty(device.Failures);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithoutSerialNumber_ThrowsDomainException(string serialNumber)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new MonitoringDevice(new RegisterDeviceCommand(serialNumber, DeviceType.Camera)));
    }

    [Fact]
    public void Constructor_WithUndefinedType_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new MonitoringDevice(new RegisterDeviceCommand("X-1", (DeviceType)42)));
    }

    [Fact]
    public void AssignTo_ValidOperatorAndVehicle_StoresAssignment()
    {
        // Arrange
        var device = RegisterWearable();
        var operatorId = Guid.NewGuid();

        // Act
        device.AssignTo(operatorId, " CAT-797F ");

        // Assert
        Assert.Equal(operatorId, device.AssignedOperatorId);
        Assert.Equal("CAT-797F", device.VehicleCode);
    }

    [Theory]
    [InlineData(false, "CAT-01")]
    [InlineData(true, "")]
    public void AssignTo_WithoutOperatorOrVehicle_ThrowsDomainException(bool hasOperator, string vehicleCode)
    {
        // Arrange
        var device = RegisterWearable();
        var operatorId = hasOperator ? Guid.NewGuid() : Guid.Empty;

        // Act & Assert
        Assert.Throws<DomainException>(() => device.AssignTo(operatorId, vehicleCode));
    }

    [Fact]
    public void ReportHeartbeat_WithLowBattery_DegradesDeviceAndRecordsFailure()
    {
        // Arrange
        var device = RegisterWearable();

        // Act
        device.ReportHeartbeat(15, Now);

        // Assert
        Assert.Equal(DeviceStatus.Degraded, device.Status);
        Assert.Equal(15, device.BatteryLevel);
        Assert.Equal(Now, device.LastSeenAt);
        var failure = Assert.Single(device.Failures);
        Assert.Equal(DeviceFailure.LowBattery, failure.Kind);
        Assert.True(failure.IsOpen);
    }

    [Fact]
    public void ReportHeartbeat_AfterCharging_BecomesAvailableAndClosesLowBattery()
    {
        // Arrange
        var device = RegisterWearable();
        device.ReportHeartbeat(15, Now);

        // Act
        device.ReportHeartbeat(80, Now.AddHours(1));

        // Assert
        Assert.Equal(DeviceStatus.Available, device.Status);
        Assert.Equal(Now.AddHours(1), Assert.Single(device.Failures).RecoveredAt);
    }

    [Fact]
    public void ReportHeartbeat_WiredDevice_StaysAvailable()
    {
        // Arrange
        var device = new MonitoringDevice(new RegisterDeviceCommand("GW-01", DeviceType.EdgeGateway));

        // Act
        device.ReportHeartbeat(null, Now);

        // Assert
        Assert.Equal(DeviceStatus.Available, device.Status);
        Assert.Empty(device.Failures);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ReportHeartbeat_BatteryOutOfRange_ThrowsDomainException(int batteryLevel)
    {
        // Arrange
        var device = RegisterWearable();

        // Act & Assert
        Assert.Throws<DomainException>(() => device.ReportHeartbeat(batteryLevel, Now));
    }

    [Fact]
    public void ReportDisconnectionThenRecovery_RecordsClosedFailure()
    {
        // Arrange
        var device = RegisterWearable();
        device.ReportHeartbeat(70, Now);

        // Act
        device.ReportDisconnection(Now.AddMinutes(10), "Signal lost");
        var statusWhileDisconnected = device.Status;
        device.ReportRecovery(Now.AddMinutes(25));

        // Assert
        Assert.Equal(DeviceStatus.Disconnected, statusWhileDisconnected);
        Assert.Equal(DeviceStatus.Available, device.Status);
        Assert.Equal(Now.AddMinutes(25), device.LastSeenAt);
        var failure = Assert.Single(device.Failures);
        Assert.Equal(DeviceFailure.Disconnection, failure.Kind);
        Assert.Equal("Signal lost", failure.Detail);
        Assert.Equal(Now.AddMinutes(10), failure.OccurredAt);
        Assert.Equal(Now.AddMinutes(25), failure.RecoveredAt);
    }

    [Fact]
    public void ReportRecovery_WithLowBattery_ReturnsToDegraded()
    {
        // Arrange
        var device = RegisterWearable();
        device.ReportHeartbeat(10, Now);
        device.ReportDisconnection(Now.AddMinutes(1), null);

        // Act
        device.ReportRecovery(Now.AddMinutes(2));

        // Assert
        Assert.Equal(DeviceStatus.Degraded, device.Status);
    }

    [Fact]
    public void ReportDisconnection_Twice_ThrowsDomainException()
    {
        // Arrange
        var device = RegisterWearable();
        device.ReportDisconnection(Now, null);

        // Act & Assert
        Assert.Throws<DomainException>(() => device.ReportDisconnection(Now.AddMinutes(1), null));
        Assert.Single(device.Failures);
    }

    [Fact]
    public void ReportRecovery_WithoutDisconnection_ThrowsDomainException()
    {
        // Arrange
        var device = RegisterWearable();

        // Act & Assert
        Assert.Throws<DomainException>(() => device.ReportRecovery(Now));
    }

    [Fact]
    public void ReportHeartbeat_WhileDisconnected_RecordsRecovery()
    {
        // Arrange
        var device = RegisterWearable();
        device.ReportDisconnection(Now, "Out of range");

        // Act
        device.ReportHeartbeat(60, Now.AddMinutes(5));

        // Assert
        Assert.Equal(DeviceStatus.Available, device.Status);
        Assert.Equal(Now.AddMinutes(5), Assert.Single(device.Failures).RecoveredAt);
    }
}
