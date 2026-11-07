namespace FatigueDetectionService.Interfaces.REST.Resources;

public record FatigueAssessmentResource(
    Guid Id,
    Guid OperatorId,
    Guid MonitoringSessionId,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs,
    string RiskLevel,
    bool RequiresAlert,
    DateTimeOffset AssessedAt);
