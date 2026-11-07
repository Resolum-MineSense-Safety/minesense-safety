using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Domain.Model.Queries;

public record GetAccessLogQuery(
    Guid? OperatorId,
    Guid? UserId,
    string? ResourceType,
    DateTimeOffset? From,
    DateTimeOffset? To,
    AccessDecision? Decision,
    int Page,
    int PageSize);
