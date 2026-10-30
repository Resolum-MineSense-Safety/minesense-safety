namespace DeviceManagementService.Interfaces.REST.Resources;

public record PreShiftAvailabilityResource(
    string Status,
    string Recommendation,
    IReadOnlyCollection<DeviceResource> EvaluatedDevices,
    IReadOnlyCollection<string> MissingDeviceTypes);
