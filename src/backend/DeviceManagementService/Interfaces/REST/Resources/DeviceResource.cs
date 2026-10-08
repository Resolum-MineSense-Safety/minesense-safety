namespace DeviceManagementService.Interfaces.REST.Resources;

public record DeviceResource(
    Guid Id,
    string SerialNumber,
    string Type,
    Guid? AssignedOperatorId,
    string? VehicleCode,
    string Status,
    int? BatteryLevel,
    DateTimeOffset? LastSeenAt,
    IReadOnlyList<DeviceFailureResource> Failures);
