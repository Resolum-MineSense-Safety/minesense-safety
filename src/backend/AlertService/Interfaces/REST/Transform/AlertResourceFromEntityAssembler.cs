using AlertService.Domain.Model.Aggregates;
using AlertService.Interfaces.REST.Resources;

namespace AlertService.Interfaces.REST.Transform;

public static class AlertResourceFromEntityAssembler
{
    public static AlertResource ToResourceFromEntity(Alert alert) =>
        new(alert.Id, alert.OperatorId, alert.AssessmentId, alert.Severity.ToString(),
            alert.Status.ToString(), alert.IssuedAt, alert.AcknowledgedAt);
}
