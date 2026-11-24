using PrivacyAuditService.Domain.Model.ReadModels;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class IncidentHistoryEntryResourceFromEntityAssembler
{
    public static IncidentHistoryEntryResource ToResourceFromEntity(IncidentHistoryEntry entry) =>
        new(entry.OccurredAt, entry.Kind, entry.Actor, entry.Summary);
}
