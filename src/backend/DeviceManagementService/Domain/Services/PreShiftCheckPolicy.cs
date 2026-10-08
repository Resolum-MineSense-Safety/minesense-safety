using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Services;

// Sprint 1 · T01 (Jhosep Argomedo): pre-shift verification checklist (indispensable devices and results).
// Remaining: confirm with the team the indispensable devices and the battery threshold.

/// <summary>
/// Checklist rules for the pre-shift verification (US15): which devices are
/// indispensable, how each device is evaluated and the resulting protection level.
/// </summary>
public static class PreShiftCheckPolicy
{
    public const string ChargeBatteryAction = "Cargar o reemplazar la batería del dispositivo";
    public const string RequestReplacementAction = "Solicitar un dispositivo de reemplazo antes de iniciar el turno";
    public const string CheckConnectionAction = "Verificar la conexión del dispositivo o solicitar un reemplazo antes de iniciar el turno";
    public const string RequestWearableAction = "Solicitar el wearable asignado; sin él la protección será limitada";

    // Team decision to confirm: devices indispensable for full protection. Wearable is optional.
    public static readonly IReadOnlyList<DeviceType> IndispensableDevices =
        [DeviceType.EdgeGateway, DeviceType.Camera, DeviceType.VibrationAlarm];

    public static readonly IReadOnlyList<DeviceType> OptionalDevices = [DeviceType.Wearable];

    public static bool IsIndispensable(DeviceType type) => IndispensableDevices.Contains(type);

    /// <summary>Evaluates every checklist device type against the devices assigned to the operator.</summary>
    public static IReadOnlyList<PreShiftCheckItem> Evaluate(IEnumerable<MonitoringDevice> assignedDevices)
    {
        var devices = assignedDevices.ToList();
        return IndispensableDevices.Concat(OptionalDevices)
            .Select(type => EvaluateType(type, devices.Where(device => device.Type == type)))
            .ToList();
    }

    /// <summary>Missing indispensable device: NotAvailable; any other warning: Limited; otherwise Full.</summary>
    public static ProtectionLevel DetermineProtection(IEnumerable<PreShiftCheckItem> items)
    {
        var list = items.ToList();
        var indispensableMissing = IndispensableDevices.Any(type =>
            !list.Any(item => item.DeviceType == type && item.Result != CheckResult.Missing));
        if (indispensableMissing) return ProtectionLevel.NotAvailable;
        if (list.Any(item => item.Result != CheckResult.Ok)) return ProtectionLevel.Limited;
        return ProtectionLevel.Full;
    }

    private static PreShiftCheckItem EvaluateType(DeviceType type, IEnumerable<MonitoringDevice> devices)
    {
        // Prefer the healthiest device when several of the same type are assigned.
        var device = devices.OrderBy(candidate => candidate.Status).FirstOrDefault();
        var indispensable = IsIndispensable(type);

        if (device is null)
            return indispensable
                ? new PreShiftCheckItem(type, null, CheckResult.Missing, RequestReplacementAction)
                : new PreShiftCheckItem(type, null, CheckResult.Warning, RequestWearableAction);

        return device.Status switch
        {
            DeviceStatus.Available => new PreShiftCheckItem(type, device.SerialNumber, CheckResult.Ok, null),
            DeviceStatus.Degraded => new PreShiftCheckItem(type, device.SerialNumber, CheckResult.Warning, ChargeBatteryAction),
            _ => new PreShiftCheckItem(type, device.SerialNumber,
                indispensable ? CheckResult.Missing : CheckResult.Warning, CheckConnectionAction)
        };
    }
}
