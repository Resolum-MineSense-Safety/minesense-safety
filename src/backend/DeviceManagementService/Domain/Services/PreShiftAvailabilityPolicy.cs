using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

public static class PreShiftAvailabilityPolicy
{
    public const string RequiredDeviceType = "camera";

    public static PreShiftAvailabilityResult Evaluate(
        IEnumerable<Device> devices, BatteryThreshold batteryThreshold)
    {
        ArgumentNullException.ThrowIfNull(devices);

        var evaluatedDevices = devices
            .Where(device => string.Equals(device.DeviceType, RequiredDeviceType,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (evaluatedDevices.Count == 0)
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.ProtectionIncomplete,
                PreShiftRecommendation.DoNotStartMonitoring,
                evaluatedDevices);
        }

        if (evaluatedDevices.Any(device =>
                device.Status != DeviceStatus.Available || device.BatteryLevel is null))
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.ProtectionIncomplete,
                PreShiftRecommendation.DoNotStartMonitoring,
                evaluatedDevices);
        }

        if (evaluatedDevices.Any(device =>
                device.BatteryLevel!.Value.Percentage < batteryThreshold.Percentage))
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.Warning,
                PreShiftRecommendation.ReviewBatteryBeforeStarting,
                evaluatedDevices);
        }

        return new PreShiftAvailabilityResult(
            PreShiftAvailabilityStatus.Available,
            PreShiftRecommendation.StartMonitoring,
            evaluatedDevices);
    }
}
