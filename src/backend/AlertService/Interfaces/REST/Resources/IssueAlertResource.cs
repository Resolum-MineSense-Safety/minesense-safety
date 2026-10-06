namespace AlertService.Interfaces.REST.Resources;

public record IssueAlertResource(Guid OperatorId, Guid AssessmentId, string Severity);
