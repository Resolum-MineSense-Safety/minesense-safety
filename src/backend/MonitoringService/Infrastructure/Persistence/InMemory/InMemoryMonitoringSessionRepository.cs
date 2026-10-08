using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace MonitoringService.Infrastructure.Persistence.InMemory;

// Sprint 1 · T06 (Joseph Huamani): registers the monitoring session (operator, vehicle, assignment / shift context, start time). Remaining: PostgreSQL adapter replacing this in-memory store.
public class InMemoryMonitoringSessionRepository : InMemoryRepository<MonitoringSession>, IMonitoringSessionRepository
{
    public Task<MonitoringSession?> FindOpenByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult(Store.Values
            .Where(session => session.OperatorId == operatorId && session.IsOpen)
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefault());

    public Task<MonitoringSession?> FindLatestByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult(Store.Values
            .Where(session => session.OperatorId == operatorId)
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefault());

    public Task<IEnumerable<MonitoringSession>> FindByStatusAsync(MonitoringStatus? status) =>
        Task.FromResult<IEnumerable<MonitoringSession>>(Store.Values
            .Where(session => status is null || session.Status == status)
            .OrderByDescending(session => session.StartedAt)
            .ToList());
}
