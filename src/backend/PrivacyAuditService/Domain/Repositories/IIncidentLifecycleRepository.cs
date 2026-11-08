using PrivacyAuditService.Domain.Model.ReadModels;

namespace PrivacyAuditService.Domain.Repositories;

/// <summary>
/// Lifecycle steps of incidents (opened, closed) received from Incident Management events.
/// Append-only: they complete the access-audit steps of the incident traceability (US23).
/// </summary>
public interface IIncidentLifecycleRepository
{
    /// <summary>Appends the step once; a step with the same key (event id) is ignored.</summary>
    Task AppendAsync(Guid incidentId, Guid stepKey, IncidentHistoryEntry entry);

    Task<IEnumerable<IncidentHistoryEntry>> FindByIncidentIdAsync(Guid incidentId);
}
