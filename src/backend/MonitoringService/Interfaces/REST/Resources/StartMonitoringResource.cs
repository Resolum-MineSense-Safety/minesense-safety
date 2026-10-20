using System.Text.Json.Serialization;

namespace MonitoringService.Interfaces.REST.Resources;

public record StartMonitoringResource(
    [property: JsonRequired] Guid OperatorId,
    string VehicleCode,
    IReadOnlyList<string>? AvailableSignals);
