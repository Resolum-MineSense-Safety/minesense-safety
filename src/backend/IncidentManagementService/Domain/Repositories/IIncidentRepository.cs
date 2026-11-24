using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace IncidentManagementService.Domain.Repositories;

public interface IIncidentRepository : IBaseRepository<Incident>
{
    /// <summary>Returns the incidents that match the filters, oldest first (null = any).</summary>
    Task<IEnumerable<Incident>> FindAsync(IncidentStatus? status, Guid? operatorId);

    /// <summary>Incident already opened for the given alert, if any.</summary>
    Task<Incident?> FindByAlertIdAsync(Guid alertId);
}
