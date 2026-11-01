using System.Collections.Concurrent;
using PrivacyAuditService.Domain.Model.ReadModels;
using PrivacyAuditService.Domain.Repositories;

namespace PrivacyAuditService.Infrastructure.Persistence.InMemory;

public class InMemoryIncidentLifecycleRepository : IIncidentLifecycleRepository
{
    private readonly ConcurrentDictionary<Guid, (Guid IncidentId, IncidentHistoryEntry Entry)> _steps = new();

    public Task AppendAsync(Guid incidentId, Guid stepKey, IncidentHistoryEntry entry)
    {
        _steps.TryAdd(stepKey, (incidentId, entry));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<IncidentHistoryEntry>> FindByIncidentIdAsync(Guid incidentId) =>
        Task.FromResult<IEnumerable<IncidentHistoryEntry>>(
            _steps.Values.Where(step => step.IncidentId == incidentId).Select(step => step.Entry).ToList());
}
