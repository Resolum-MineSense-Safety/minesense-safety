using FleetMonitoringService.Domain.Model.ValueObjects;

namespace FleetMonitoringService.Domain.Model.Queries;

/// <summary>
/// Fleet status filter for the control center. Every non-null criterion is combined with AND.
/// </summary>
public record GetFleetStatusQuery(
    Shift? Shift = null,
    string? Fleet = null,
    string? Location = null,
    RiskLevel? RiskLevel = null);
