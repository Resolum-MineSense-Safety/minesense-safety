namespace FatigueDetectionService.Domain.Model.Commands;

/// <param name="AssessmentId">Id of the reading in the Edge, which makes recording it idempotent.</param>
/// <param name="CapturedAt">When the cabin captured the reading.</param>
public record RecordEdgeAssessmentCommand(
    Guid AssessmentId,
    Guid OperatorId,
    Guid MonitoringSessionId,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs,
    DateTimeOffset CapturedAt);
