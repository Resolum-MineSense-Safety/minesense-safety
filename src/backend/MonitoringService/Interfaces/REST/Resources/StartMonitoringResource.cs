using System.Text.Json.Serialization;

namespace MonitoringService.Interfaces.REST.Resources;

public record StartMonitoringResource(
    [property: JsonRequired] Guid OperatorId,
    string VehicleCode,
    Guid? PreShiftCheckId,
    IReadOnlyList<string>? AvailableSignals);
