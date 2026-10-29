namespace DeviceManagementService.Interfaces.REST.Resources;

public record OperatorAvailabilityResource(
    Guid OperatorId,
    string Status,
    IReadOnlyCollection<DeviceResource> Devices);
