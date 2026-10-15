namespace PrivacyAuditService.Domain.Model.Commands;

public record RegisterAccessAttemptCommand(
    Guid UserId,
    string ResourceType,
    Guid ResourceId,
    IEnumerable<string> Fields,
    string Purpose);
