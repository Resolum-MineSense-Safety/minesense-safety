namespace AnalyticsService.Domain.Model.ReadModels;

/// <summary>
/// Local copy of where an operator works, fed by the <c>FleetOperatorUpdated</c> events of Fleet
/// Monitoring (ADR-005: no queries to another service's database). It enriches the
/// <c>AlertIssued</c> events, which only carry the operator id. The mine site is the id of the
/// operator's mining unit, the only reference Fleet Monitoring keeps.
/// </summary>
public record OperatorContext(
    Guid OperatorId,
    string Shift,
    string MineSite,
    string Location,
    string Fleet,
    DateTimeOffset UpdatedAt)
{
    /// <summary>Value recorded for a dimension Fleet Monitoring has not reported yet.</summary>
    public const string Unknown = "unknown";
}
