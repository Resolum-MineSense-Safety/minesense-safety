using AlertService.Domain.Model.ValueObjects;

namespace AlertService.Domain.Model.Commands;

/// <param name="AssessmentId">Id of the reading in the Edge, which is also the id of its assessment.</param>
/// <param name="IssuedAt">When the cabin warned the operator.</param>
public record RecordCabinWarningCommand(
    Guid OperatorId,
    Guid AssessmentId,
    AlertSeverity Severity,
    DateTimeOffset IssuedAt);
