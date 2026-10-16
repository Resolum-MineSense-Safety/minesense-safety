using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Model.Commands;

public record ReportDeviceStatusCommand(
    DeviceStatus Status,
    BatteryLevel BatteryLevel,
    DateTimeOffset ReportedAt,
    IReadOnlyCollection<string> AvailableComponents);
