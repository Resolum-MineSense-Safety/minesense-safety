using FleetMonitoringService.Domain.Model.Aggregates;

namespace FleetMonitoringService.Application.Internal.IntegrationEvents;

/// <summary>
/// Published on <c>minesense.fleet.operator-updated</c> whenever a monitored operator is registered,
/// reassigned or changes risk level. Analytics and Feedback &amp; Reliability keep their own copy of
/// this operational context (shift, fleet, location, mining unit) to enrich the alerts they receive.
/// The operator's name is not included: other contexts only need the operator id.
/// </summary>
public record FleetOperatorUpdatedEvent(
    Guid OperatorId,
    string VehicleCode,
    string Fleet,
    Guid? MiningUnitId,
    Guid? FleetId,
    string Shift,
    string Location,
    string RiskLevel,
    DateTimeOffset UpdatedAt)
{
    public const string Type = "FleetOperatorUpdated";

    public static FleetOperatorUpdatedEvent FromOperator(MonitoredOperator monitoredOperator) =>
        new(monitoredOperator.OperatorId, monitoredOperator.VehicleCode, monitoredOperator.Fleet,
            monitoredOperator.MiningUnitId, monitoredOperator.FleetId, monitoredOperator.Shift.ToString(),
            monitoredOperator.Location, monitoredOperator.CurrentRiskLevel.ToString(), monitoredOperator.LastUpdatedAt);
}

/// <summary>Fields of <c>FatigueAssessed</c> (Fatigue Detection) that Fleet Monitoring needs.</summary>
public record FatigueAssessedEvent(Guid AssessmentId, Guid OperatorId, string RiskLevel, DateTimeOffset AssessedAt);
