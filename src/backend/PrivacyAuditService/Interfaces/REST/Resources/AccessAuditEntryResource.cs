namespace PrivacyAuditService.Interfaces.REST.Resources;

public record AccessAuditEntryResource(
    Guid Id,
    Guid UserId,
    string ResourceType,
    Guid ResourceId,
    IEnumerable<string> Fields,
    string Purpose,
    string Decision,
    DateTimeOffset AccessedAt,
    string Hash);
