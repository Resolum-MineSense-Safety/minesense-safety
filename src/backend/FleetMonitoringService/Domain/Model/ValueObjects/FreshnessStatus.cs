namespace FleetMonitoringService.Domain.Model.ValueObjects;

/// <summary>Indicates whether the control-center status still represents recent data.</summary>
public enum FreshnessStatus
{
    Current,
    Stale
}
