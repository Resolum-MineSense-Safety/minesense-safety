using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace IncidentManagementService.Infrastructure.Persistence.InMemory;

public class InMemoryIncidentRepository : InMemoryRepository<Incident>, IIncidentRepository
{
    public Task<IEnumerable<Incident>> FindAsync(IncidentStatus? status, Guid? operatorId) =>
        Task.FromResult<IEnumerable<Incident>>(
            Store.Values
                .Where(incident => status is null || incident.Status == status)
                .Where(incident => operatorId is null || incident.OperatorId == operatorId)
                .OrderBy(incident => incident.OpenedAt)
                .ToList());

    public Task<Incident?> FindByAlertIdAsync(Guid alertId) =>
        Task.FromResult(Store.Values.FirstOrDefault(incident => incident.AlertId == alertId));
}
