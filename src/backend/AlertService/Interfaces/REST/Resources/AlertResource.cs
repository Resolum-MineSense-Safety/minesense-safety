namespace AlertService.Interfaces.REST.Resources;

public record AlertResource(
    Guid Id,
    Guid OperatorId,
    Guid AssessmentId,
    string Severity,
    string Status,
    DateTimeOffset IssuedAt,
    DateTimeOffset? AcknowledgedAt);
