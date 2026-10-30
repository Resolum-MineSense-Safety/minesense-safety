using System.Text.Json.Serialization;

namespace FatigueDetectionService.Interfaces.REST.Resources;

public record AssessFatigueResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] Guid MonitoringSessionId,
    [property: JsonRequired] double Perclos,
    [property: JsonRequired] double BlinkRatePerMinute,
    [property: JsonRequired] double HeartRateVariabilityMs
);
