using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class AccessAuditEntryResourceFromEntityAssembler
{
    public static AccessAuditEntryResource ToResourceFromEntity(AccessAuditEntry entry) =>
        new(entry.Id, entry.UserId, entry.ResourceType, entry.ResourceId, entry.Fields,
            entry.Purpose, entry.Decision.ToString(), entry.AccessedAt, entry.Hash);
}
