namespace MonitoringService.Interfaces.REST.Resources;

public record MonitoringSessionResource(
    Guid Id,
    Guid OperatorId,
    string VehicleCode,
    Guid AssignmentId,
    Guid? PreShiftCheckId,
    string Status,
    IReadOnlyList<string> AvailableSignals,
    IReadOnlyList<string> MissingSignals,
    DateTimeOffset StartedAt,
    DateTimeOffset? StoppedAt,
    IReadOnlyList<SignalEventResource> SignalEvents);
