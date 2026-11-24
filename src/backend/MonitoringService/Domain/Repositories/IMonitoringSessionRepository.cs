using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace MonitoringService.Domain.Repositories;

public interface IMonitoringSessionRepository : IBaseRepository<MonitoringSession>
{
    Task<MonitoringSession?> FindOpenByOperatorIdAsync(Guid operatorId);
    Task<MonitoringSession?> FindLatestByOperatorIdAsync(Guid operatorId);
    Task<IEnumerable<MonitoringSession>> FindByStatusAsync(MonitoringStatus? status);
}
