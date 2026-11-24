namespace DeviceManagementService.Interfaces.REST.Resources;

public record DeviceResource(
    Guid Id,
    string DeviceCode,
    string DeviceType,
    Guid OrganizationId,
    Guid? AssignedOperatorId,
    Guid? AssignedVehicleId,
    string? Status,
    decimal? BatteryLevel,
    DateTimeOffset? ReportedAt,
    IReadOnlyCollection<string> AvailableComponents);
