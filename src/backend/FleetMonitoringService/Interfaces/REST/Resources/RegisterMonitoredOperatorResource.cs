using System.Text.Json.Serialization;

namespace FleetMonitoringService.Interfaces.REST.Resources;

public record RegisterMonitoredOperatorResource(
    [property: JsonRequired] Guid OperatorId,
    string FullName,
    string VehicleCode,
    string Fleet,
    string Shift,
    string Location);
