using System.Text.Json.Serialization;

namespace DeviceManagementService.Interfaces.REST.Resources;

public record ReportDeviceStatusResource(
    [property: JsonRequired] string Status,
    [property: JsonRequired] decimal BatteryLevel,
    [property: JsonRequired] DateTimeOffset ReportedAt,
    [property: JsonRequired] IReadOnlyCollection<string> AvailableComponents);
