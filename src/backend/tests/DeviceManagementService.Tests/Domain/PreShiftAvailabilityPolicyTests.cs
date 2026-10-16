using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Domain;

public class PreShiftAvailabilityPolicyTests
{
    private readonly BatteryThreshold _batteryThreshold = new(50);

    [Fact]
    public void Evaluate_WithAvailableCameraAboveConfiguredThreshold_AllowsMonitoring()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 80);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Available, result.Status);
        Assert.Equal(PreShiftRecommendation.StartMonitoring, result.Recommendation);
        Assert.Single(result.EvaluatedDevices);
    }

    [Fact]
    public void Evaluate_WithCameraBelowConfiguredThreshold_ReturnsBatteryWarning()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 30);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Warning, result.Status);
        Assert.Equal(PreShiftRecommendation.ReviewBatteryBeforeStarting, result.Recommendation);
    }

    [Fact]
    public void Evaluate_WithoutRequiredCamera_ReportsIncompleteProtection()
    {
        var wearable = CreateDevice("wearable-01", "wearable", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
        Assert.Empty(result.EvaluatedDevices);
    }

    [Fact]
    public void Evaluate_WithInterruptedCamera_ReportsIncompleteProtection()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Interrupted, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void BatteryThreshold_WithInvalidPercentage_ThrowsDomainException(decimal percentage)
    {
        Assert.Throws<DomainException>(() => new BatteryThreshold(percentage));
    }

    private static Device CreateCamera(string code, DeviceStatus status, decimal battery) =>
        CreateDevice(code, PreShiftAvailabilityPolicy.RequiredDeviceType, status, battery);

    private static Device CreateDevice(
        string code, string type, DeviceStatus status, decimal battery)
    {
        var device = new Device(new RegisterDeviceCommand(code, type, Guid.NewGuid()));
        device.ReportStatus(new ReportDeviceStatusCommand(
            device.Id, status, new BatteryLevel(battery), DateTimeOffset.UtcNow, []));
        return device;
    }
}
