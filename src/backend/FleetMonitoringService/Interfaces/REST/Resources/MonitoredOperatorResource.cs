namespace FleetMonitoringService.Interfaces.REST.Resources;

public record MonitoredOperatorResource(
    Guid Id,
    Guid OperatorId,
    string FullName,
    string VehicleCode,
    string Fleet,
    Guid? MiningUnitId,
    Guid? FleetId,
    string Shift,
    string Location,
    string CurrentRiskLevel,
    DateTimeOffset LastUpdatedAt,
    string Freshness,
    bool IsStale);
