using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace IncidentManagementService.Domain.Repositories;

public interface IIncidentRepository : IBaseRepository<Incident>
{
    Task<IEnumerable<Incident>> FindByStatusAsync(IncidentStatus? status);
    Task<IEnumerable<Incident>> FindByOperatorIdAsync(Guid operatorId);
}
