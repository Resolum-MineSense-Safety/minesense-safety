using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Domain;

public class PreShiftCheckPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 6, 0, 0, TimeSpan.Zero);

    private static MonitoringDevice Device(DeviceType type, int? batteryLevel = 90)
    {
        var device = new MonitoringDevice(new RegisterDeviceCommand($"{type}-{Guid.NewGuid():N}", type));
        device.ReportHeartbeat(batteryLevel, Now);
        return device;
    }

    private static List<MonitoringDevice> FullSet() =>
    [
        Device(DeviceType.EdgeGateway, null),
        Device(DeviceType.Camera, null),
        Device(DeviceType.VibrationAlarm),
        Device(DeviceType.Wearable)
    ];

    private static ProtectionLevel Evaluate(IEnumerable<MonitoringDevice> devices) =>
        PreShiftCheckPolicy.DetermineProtection(PreShiftCheckPolicy.Evaluate(devices));

    [Fact]
    public void Evaluate_AllDevicesAvailable_ReturnsFullProtection()
    {
        // Arrange
        var devices = FullSet();

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        Assert.Equal(4, items.Count);
        Assert.All(items, item => Assert.Equal(CheckResult.Ok, item.Result));
        Assert.All(items, item => Assert.Null(item.RecommendedAction));
        Assert.Equal(ProtectionLevel.Full, PreShiftCheckPolicy.DetermineProtection(items));
    }

    [Fact]
    public void Evaluate_DeviceWithLowBattery_ReturnsLimitedWithChargeAction()
    {
        // Arrange
        var devices = FullSet();
        devices[2].ReportHeartbeat(10, Now.AddMinutes(1));

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        var alarm = items.Single(item => item.DeviceType == DeviceType.VibrationAlarm);
        Assert.Equal(CheckResult.Warning, alarm.Result);
        Assert.Equal(PreShiftCheckPolicy.ChargeBatteryAction, alarm.RecommendedAction);
        Assert.Equal(ProtectionLevel.Limited, PreShiftCheckPolicy.DetermineProtection(items));
    }

    [Fact]
    public void Evaluate_MissingWearable_ReturnsLimited()
    {
        // Arrange
        var devices = FullSet().Where(device => device.Type != DeviceType.Wearable);

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        var wearable = items.Single(item => item.DeviceType == DeviceType.Wearable);
        Assert.Equal(CheckResult.Warning, wearable.Result);
        Assert.Equal(PreShiftCheckPolicy.RequestWearableAction, wearable.RecommendedAction);
        Assert.Equal(ProtectionLevel.Limited, PreShiftCheckPolicy.DetermineProtection(items));
    }

    [Theory]
    [InlineData(DeviceType.Camera)]
    [InlineData(DeviceType.EdgeGateway)]
    [InlineData(DeviceType.VibrationAlarm)]
    public void Evaluate_MissingIndispensableDevice_ReturnsNotAvailable(DeviceType missingType)
    {
        // Arrange
        var devices = FullSet().Where(device => device.Type != missingType);

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        var missing = items.Single(item => item.DeviceType == missingType);
        Assert.Equal(CheckResult.Missing, missing.Result);
        Assert.Null(missing.SerialNumber);
        Assert.Equal(PreShiftCheckPolicy.RequestReplacementAction, missing.RecommendedAction);
        Assert.Equal(ProtectionLevel.NotAvailable, PreShiftCheckPolicy.DetermineProtection(items));
    }

    [Fact]
    public void Evaluate_DisconnectedCamera_IsMissingAndNotAvailable()
    {
        // Arrange
        var devices = FullSet();
        devices[1].ReportDisconnection(Now.AddMinutes(1), "Cable unplugged");

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        var camera = items.Single(item => item.DeviceType == DeviceType.Camera);
        Assert.Equal(CheckResult.Missing, camera.Result);
        Assert.Equal(PreShiftCheckPolicy.CheckConnectionAction, camera.RecommendedAction);
        Assert.Equal(ProtectionLevel.NotAvailable, PreShiftCheckPolicy.DetermineProtection(items));
    }

    [Fact]
    public void Evaluate_DisconnectedWearable_IsWarningAndLimited()
    {
        // Arrange
        var devices = FullSet();
        devices[3].ReportDisconnection(Now.AddMinutes(1), null);

        // Act
        var protection = Evaluate(devices);

        // Assert
        Assert.Equal(ProtectionLevel.Limited, protection);
    }

    [Fact]
    public void Evaluate_DuplicatedType_UsesHealthiestDevice()
    {
        // Arrange
        var devices = FullSet();
        var spareCamera = Device(DeviceType.Camera);
        devices[1].ReportDisconnection(Now.AddMinutes(1), null);
        devices.Add(spareCamera);

        // Act
        var items = PreShiftCheckPolicy.Evaluate(devices);

        // Assert
        var camera = items.Single(item => item.DeviceType == DeviceType.Camera);
        Assert.Equal(spareCamera.SerialNumber, camera.SerialNumber);
        Assert.Equal(CheckResult.Ok, camera.Result);
    }

    [Fact]
    public void DetermineProtection_WithoutItems_ReturnsNotAvailable()
    {
        // Act
        var protection = PreShiftCheckPolicy.DetermineProtection([]);

        // Assert
        Assert.Equal(ProtectionLevel.NotAvailable, protection);
    }

    [Fact]
    public void PreShiftCheck_WithFullSet_CanStartMonitoring()
    {
        // Arrange
        var items = PreShiftCheckPolicy.Evaluate(FullSet());

        // Act
        var check = new PreShiftCheck(Guid.NewGuid(), " CAT-01 ", Now, items);

        // Assert
        Assert.Equal(ProtectionLevel.Full, check.Protection);
        Assert.True(check.CanStartMonitoring);
        Assert.Equal("CAT-01", check.VehicleCode);
        Assert.Equal(Now, check.CheckedAt);
    }

    [Fact]
    public void PreShiftCheck_WithoutDevices_CannotStartMonitoring()
    {
        // Act
        var check = new PreShiftCheck(Guid.NewGuid(), "CAT-01", Now, PreShiftCheckPolicy.Evaluate([]));

        // Assert
        Assert.Equal(ProtectionLevel.NotAvailable, check.Protection);
        Assert.False(check.CanStartMonitoring);
    }

    [Theory]
    [InlineData(false, "CAT-01")]
    [InlineData(true, " ")]
    public void PreShiftCheck_WithoutOperatorOrVehicle_ThrowsDomainException(bool hasOperator, string vehicleCode)
    {
        // Arrange
        var operatorId = hasOperator ? Guid.NewGuid() : Guid.Empty;

        // Act & Assert
        Assert.Throws<DomainException>(() => new PreShiftCheck(operatorId, vehicleCode, Now, []));
    }
}
