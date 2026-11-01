namespace AlertService.Application.Internal.IntegrationEvents;

/// <summary>Published on <c>minesense.alerts.issued</c> when a new alert is delivered in the cabin.</summary>
public record AlertIssuedEvent(
    Guid AlertId,
    Guid OperatorId,
    Guid AssessmentId,
    string Severity,
    DateTimeOffset IssuedAt,
    string Origin = "Live")
{
    public const string Type = "AlertIssued";
}

/// <summary>Published on <c>minesense.alerts.acknowledged</c> when the operator confirms the alert.</summary>
public record AlertAcknowledgedEvent(Guid AlertId, Guid OperatorId, string Severity, DateTimeOffset AcknowledgedAt)
{
    public const string Type = "AlertAcknowledged";
}

/// <summary>Published on <c>minesense.alerts.escalated</c> when an unacknowledged alert goes to the supervisor.</summary>
public record AlertEscalatedEvent(Guid AlertId, Guid OperatorId, string Severity, DateTimeOffset EscalatedAt)
{
    public const string Type = "AlertEscalated";
}

/// <summary>
/// Fields of <c>FatigueAssessed</c> (Fatigue Detection) that Alerts needs. Local copy: bounded
/// contexts do not share event types.
/// </summary>
public record FatigueAssessedEvent(Guid AssessmentId, Guid OperatorId, string RiskLevel, bool RequiresAlert);

/// <summary>Fields of <c>CabinWarningSynchronized</c> (Edge Synchronization) that Alerts needs.</summary>
public record CabinWarningSynchronizedEvent(Guid EdgeEventId, DateTimeOffset CapturedAt, CabinWarningData Data);

/// <summary>Payload the cabin wrote for a <c>CabinWarningIssued</c> event.</summary>
public record CabinWarningData(Guid OperatorId, string Severity, Guid? ReadingEventId);
