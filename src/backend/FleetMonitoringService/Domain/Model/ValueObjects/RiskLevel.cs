namespace FleetMonitoringService.Domain.Model.ValueObjects;

/// <summary>
/// Current risk level of an operator as seen by the control center.
/// Local copy: bounded contexts do not share this enum.
/// </summary>
public enum RiskLevel
{
    Normal,
    Warning,
    Critical
}
