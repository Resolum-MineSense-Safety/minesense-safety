using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace IncidentManagementService.Infrastructure.Persistence.InMemory;

public class InMemoryIncidentRepository : InMemoryRepository<Incident>, IIncidentRepository
{
    public Task<IEnumerable<Incident>> FindByStatusAsync(IncidentStatus? status) =>
        Task.FromResult<IEnumerable<Incident>>(
            Store.Values.Where(incident => status is null || incident.Status == status)
                .OrderBy(incident => incident.OpenedAt)
                .ToList());

    public Task<IEnumerable<Incident>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<Incident>>(
            Store.Values.Where(incident => incident.OperatorId == operatorId)
                .OrderByDescending(incident => incident.OpenedAt)
                .ToList());
}
