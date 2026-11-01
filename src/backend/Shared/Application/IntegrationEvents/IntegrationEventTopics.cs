namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Kafka topics of the cloud-to-cloud domain events (ADR-004). The event type travels in the
/// envelope, so each topic carries one kind of fact.
/// </summary>
public static class IntegrationEventTopics
{
    public const string FatigueAssessed = "minesense.fatigue.assessed";
    public const string AlertIssued = "minesense.alerts.issued";
    public const string AlertAcknowledged = "minesense.alerts.acknowledged";
    public const string AlertEscalated = "minesense.alerts.escalated";
    public const string IncidentOpened = "minesense.incidents.opened";
    public const string IncidentClosed = "minesense.incidents.closed";

    /// <summary>Snapshot of a monitored operator (shift, fleet, location, mining unit, risk level).</summary>
    public const string FleetOperatorUpdated = "minesense.fleet.operator-updated";

    public static readonly IReadOnlyList<string> All =
    [
        FatigueAssessed, AlertIssued, AlertAcknowledged, AlertEscalated, IncidentOpened, IncidentClosed,
        FleetOperatorUpdated
    ];
}
