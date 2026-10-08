namespace MonitoringService.Interfaces.REST.Resources;

// Sprint 1 · T09 (Jhosep Argomedo): contract consumed by the operator dashboard (src/frontend) through GET /api/v1/monitoring-sessions/current. Remaining: status indicator component and polling in the frontend.
/// <summary>
/// Current monitoring status of an operator (US02). <see cref="MissingSignals"/> tells the
/// dashboard which readings must not be shown; <see cref="Message"/> is the Spanish text for the operator.
/// </summary>
public record MonitoringStatusResource(
    Guid SessionId,
    Guid OperatorId,
    string VehicleCode,
    string Status,
    IReadOnlyList<string> AvailableSignals,
    IReadOnlyList<string> MissingSignals,
    string Message,
    DateTimeOffset StartedAt,
    DateTimeOffset? StoppedAt);
