using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

public static class PreShiftAvailabilityPolicy
{
    /// <summary>Cabin camera that feeds eye tracking (PERCLOS).</summary>
    public const string CameraDeviceType = "camera";

    /// <summary>Operator wearable that feeds the heart-rate signal.</summary>
    public const string WearableDeviceType = "wearable";

    /// <summary>Every type needs at least one assigned device for full protection.</summary>
    public static readonly IReadOnlyList<string> RequiredDeviceTypes =
        [CameraDeviceType, WearableDeviceType];

    public static PreShiftAvailabilityResult Evaluate(
        IEnumerable<Device> devices, BatteryThreshold batteryThreshold)
    {
        ArgumentNullException.ThrowIfNull(devices);

        var evaluatedDevices = devices
            .Where(device => RequiredDeviceTypes.Contains(device.DeviceType,
                StringComparer.OrdinalIgnoreCase))
            .ToList();

        var missingDeviceTypes = RequiredDeviceTypes
            .Where(type => !evaluatedDevices.Any(device =>
                string.Equals(device.DeviceType, type, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingDeviceTypes.Count > 0)
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.ProtectionIncomplete,
                PreShiftRecommendation.DoNotStartMonitoring,
                evaluatedDevices,
                missingDeviceTypes);
        }

        if (evaluatedDevices.Any(device =>
                device.Status != DeviceStatus.Available || device.BatteryLevel is null))
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.ProtectionIncomplete,
                PreShiftRecommendation.DoNotStartMonitoring,
                evaluatedDevices,
                missingDeviceTypes);
        }

        if (evaluatedDevices.Any(device =>
                device.BatteryLevel!.Value.Percentage < batteryThreshold.Percentage))
        {
            return new PreShiftAvailabilityResult(
                PreShiftAvailabilityStatus.Warning,
                PreShiftRecommendation.ReviewBatteryBeforeStarting,
                evaluatedDevices,
                missingDeviceTypes);
        }

        return new PreShiftAvailabilityResult(
            PreShiftAvailabilityStatus.Available,
            PreShiftRecommendation.StartMonitoring,
            evaluatedDevices,
            missingDeviceTypes);
    }
}
