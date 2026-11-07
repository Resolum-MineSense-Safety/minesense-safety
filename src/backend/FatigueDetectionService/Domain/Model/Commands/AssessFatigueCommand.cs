namespace FatigueDetectionService.Domain.Model.Commands;

public record AssessFatigueCommand(
    Guid OperatorId,
    Guid MonitoringSessionId,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs);
