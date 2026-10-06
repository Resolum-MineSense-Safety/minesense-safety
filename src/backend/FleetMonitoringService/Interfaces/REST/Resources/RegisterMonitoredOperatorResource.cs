namespace FleetMonitoringService.Interfaces.REST.Resources;

public record RegisterMonitoredOperatorResource(
    Guid OperatorId,
    string FullName,
    string VehicleCode,
    string Fleet,
    string Shift,
    string Location);
