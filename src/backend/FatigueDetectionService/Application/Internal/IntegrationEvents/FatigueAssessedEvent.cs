using FatigueDetectionService.Domain.Model.Aggregates;

namespace FatigueDetectionService.Application.Internal.IntegrationEvents;

/// <summary>
/// Published on <c>minesense.fatigue.assessed</c> after every assessment (ADR-004). Alerts issues a
/// preventive alert when <see cref="RequiresAlert"/> is true and Fleet Monitoring updates the risk.
/// </summary>
public record FatigueAssessedEvent(
    Guid AssessmentId,
    Guid OperatorId,
    Guid MonitoringSessionId,
    string RiskLevel,
    bool RequiresAlert,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs,
    DateTimeOffset AssessedAt)
{
    public const string Type = "FatigueAssessed";

    public static FatigueAssessedEvent FromAssessment(FatigueAssessment assessment) =>
        new(assessment.Id, assessment.OperatorId, assessment.MonitoringSessionId, assessment.RiskLevel.ToString(),
            assessment.RequiresAlert, assessment.Signals.Perclos, assessment.Signals.BlinkRatePerMinute,
            assessment.Signals.HeartRateVariabilityMs, assessment.AssessedAt);
}
