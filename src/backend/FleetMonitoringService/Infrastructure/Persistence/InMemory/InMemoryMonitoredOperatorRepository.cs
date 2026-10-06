using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace FleetMonitoringService.Infrastructure.Persistence.InMemory;

public class InMemoryMonitoredOperatorRepository : InMemoryRepository<MonitoredOperator>, IMonitoredOperatorRepository
{
    public Task<MonitoredOperator?> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult(Store.Values.FirstOrDefault(monitored => monitored.OperatorId == operatorId));

    public Task<IEnumerable<MonitoredOperator>> FindByCriteriaAsync(
        Shift? shift, string? fleet, string? location, RiskLevel? riskLevel) =>
        Task.FromResult<IEnumerable<MonitoredOperator>>(
            Store.Values
                .Where(monitored => shift is null || monitored.Shift == shift)
                .Where(monitored => string.IsNullOrWhiteSpace(fleet)
                                    || monitored.Fleet.Equals(fleet.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(monitored => string.IsNullOrWhiteSpace(location)
                                    || monitored.Location.Equals(location.Trim(), StringComparison.OrdinalIgnoreCase))
                .Where(monitored => riskLevel is null || monitored.CurrentRiskLevel == riskLevel)
                .OrderByDescending(monitored => monitored.CurrentRiskLevel)
                .ThenBy(monitored => monitored.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList());
}
