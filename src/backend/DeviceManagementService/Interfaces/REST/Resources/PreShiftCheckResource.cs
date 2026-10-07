namespace DeviceManagementService.Interfaces.REST.Resources;

// Sprint 1 · T03 (Carlos Onofre): verification result shown to the operator.
// Remaining: dashboard view in src/frontend.

public record PreShiftCheckResource(
    Guid Id,
    Guid OperatorId,
    string VehicleCode,
    DateTimeOffset CheckedAt,
    string Protection,
    bool CanStartMonitoring,
    IReadOnlyList<PreShiftCheckItemResource> Items);
