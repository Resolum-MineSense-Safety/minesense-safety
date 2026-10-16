using DeviceManagementService.Domain.Model.ValueObjects;

namespace DeviceManagementService.Domain.Model.Commands;

public record ReportDeviceStatusCommand(
    Guid DeviceId,
    DeviceStatus Status,
    BatteryLevel BatteryLevel,
    DateTimeOffset ReportedAt,
    IReadOnlyCollection<string> AvailableComponents);
