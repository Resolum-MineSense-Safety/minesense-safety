namespace FatigueDetectionService.Interfaces.REST.Resources;

public record AssessFatigueResource(
    Guid OperatorId,
    Guid MonitoringSessionId,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs);
