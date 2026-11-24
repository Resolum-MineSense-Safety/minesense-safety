using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Interfaces.REST.Resources;

namespace FatigueDetectionService.Interfaces.REST.Transform;

public static class FatigueAssessmentResourceFromEntityAssembler
{
    public static FatigueAssessmentResource ToResourceFromEntity(FatigueAssessment assessment) =>
        new(assessment.Id, assessment.OperatorId, assessment.MonitoringSessionId,
            assessment.Signals.Perclos, assessment.Signals.BlinkRatePerMinute,
            assessment.Signals.HeartRateVariabilityMs, assessment.RiskLevel.ToString(),
            assessment.RequiresAlert, assessment.AssessedAt);
}
