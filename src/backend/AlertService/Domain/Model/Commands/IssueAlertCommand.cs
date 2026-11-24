using AlertService.Domain.Model.ValueObjects;

namespace AlertService.Domain.Model.Commands;

public record IssueAlertCommand(Guid OperatorId, Guid AssessmentId, AlertSeverity Severity);
