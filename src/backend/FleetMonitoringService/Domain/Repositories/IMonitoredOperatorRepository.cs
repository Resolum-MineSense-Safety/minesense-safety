using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace FleetMonitoringService.Domain.Repositories;

public interface IMonitoredOperatorRepository : IBaseRepository<MonitoredOperator>
{
    Task<MonitoredOperator?> FindByOperatorIdAsync(Guid operatorId);

    /// <summary>
    /// Returns operators matching every non-null criterion, ordered by risk level (highest first) then name.
    /// </summary>
    Task<IEnumerable<MonitoredOperator>> FindByCriteriaAsync(
        Shift? shift, string? fleet, string? location, RiskLevel? riskLevel);
}
