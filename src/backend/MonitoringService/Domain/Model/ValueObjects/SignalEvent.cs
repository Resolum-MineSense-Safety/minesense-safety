namespace MonitoringService.Domain.Model.ValueObjects;

/// <summary>
/// Traceability record of a signal becoming unavailable or available again during a session.
/// </summary>
public record SignalEvent(SignalType Signal, SignalEventType Type, DateTimeOffset OccurredAt);
