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
    public void Evaluate_WithAvailableCameraAndWearableAboveConfiguredThreshold_AllowsMonitoring()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 80);
        var wearable = CreateWearable("wearable-01", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera, wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Available, result.Status);
        Assert.Equal(PreShiftRecommendation.StartMonitoring, result.Recommendation);
        Assert.Equal(2, result.EvaluatedDevices.Count);
        Assert.Empty(result.MissingDeviceTypes);
    }

    [Fact]
    public void Evaluate_WithCameraBelowConfiguredThreshold_ReturnsBatteryWarning()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 30);
        var wearable = CreateWearable("wearable-01", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera, wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Warning, result.Status);
        Assert.Equal(PreShiftRecommendation.ReviewBatteryBeforeStarting, result.Recommendation);
    }

    [Fact]
    public void Evaluate_WithWearableBelowConfiguredThreshold_ReturnsBatteryWarning()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 90);
        var wearable = CreateWearable("wearable-01", DeviceStatus.Available, 20);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera, wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Warning, result.Status);
        Assert.Equal(PreShiftRecommendation.ReviewBatteryBeforeStarting, result.Recommendation);
    }

    [Fact]
    public void Evaluate_WithoutRequiredCamera_ReportsIncompleteProtection()
    {
        var wearable = CreateWearable("wearable-01", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
        Assert.Equal([PreShiftAvailabilityPolicy.CameraDeviceType], result.MissingDeviceTypes);
    }

    [Fact]
    public void Evaluate_WithoutHeartRateWearable_ReportsIncompleteProtection()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
        Assert.Equal([PreShiftAvailabilityPolicy.WearableDeviceType], result.MissingDeviceTypes);
    }

    [Fact]
    public void Evaluate_WithUnrelatedDevicesOnly_ReportsBothTypesMissing()
    {
        var gps = CreateDevice("gps-01", "gps", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([gps], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Empty(result.EvaluatedDevices);
        Assert.Equal(PreShiftAvailabilityPolicy.RequiredDeviceTypes, result.MissingDeviceTypes);
    }

    [Fact]
    public void Evaluate_WithInterruptedWearable_ReportsIncompleteProtection()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Available, 90);
        var wearable = CreateWearable("wearable-01", DeviceStatus.Interrupted, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera, wearable], _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
        Assert.Empty(result.MissingDeviceTypes);
    }

    [Fact]
    public void Evaluate_WithInterruptedCamera_ReportsIncompleteProtection()
    {
        var camera = CreateCamera("camera-01", DeviceStatus.Interrupted, 90);
        var wearable = CreateWearable("wearable-01", DeviceStatus.Available, 90);

        var result = PreShiftAvailabilityPolicy.Evaluate([camera, wearable], _batteryThreshold);

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
        CreateDevice(code, PreShiftAvailabilityPolicy.CameraDeviceType, status, battery);

    private static Device CreateWearable(string code, DeviceStatus status, decimal battery) =>
        CreateDevice(code, PreShiftAvailabilityPolicy.WearableDeviceType, status, battery);

    private static Device CreateDevice(
        string code, string type, DeviceStatus status, decimal battery)
    {
        var device = new Device(new RegisterDeviceCommand(code, type, Guid.NewGuid()));
        device.ReportStatus(new ReportDeviceStatusCommand(
            device.Id, status, new BatteryLevel(battery), DateTimeOffset.UtcNow, []));
        return device;
    }
}
