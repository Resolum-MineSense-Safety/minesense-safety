namespace PrivacyAuditService.Interfaces.REST.Resources;

public record ConsentResource(
    Guid Id,
    Guid OperatorId,
    string PolicyVersion,
    IEnumerable<string> Purposes,
    string Status,
    bool BlocksMonitoring,
    DateTimeOffset DecidedAt);
