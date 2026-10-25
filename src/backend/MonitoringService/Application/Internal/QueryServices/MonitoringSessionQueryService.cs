using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Queries;
using MonitoringService.Domain.Repositories;
using MonitoringService.Domain.Services;

namespace MonitoringService.Application.Internal.QueryServices;

// Sprint 1 · T08 (Renato Calvo): monitoring status query (US02) — the current status of an operator comes from the open session or, if none, the latest one.
public class MonitoringSessionQueryService(IMonitoringSessionRepository sessionRepository) : IMonitoringSessionQueryService
{
    public Task<MonitoringSession?> Handle(GetSessionByIdQuery query) =>
        sessionRepository.FindByIdAsync(query.SessionId);

    public async Task<MonitoringSession?> Handle(GetCurrentStatusByOperatorQuery query) =>
        await sessionRepository.FindOpenByOperatorIdAsync(query.OperatorId)
        ?? await sessionRepository.FindLatestByOperatorIdAsync(query.OperatorId);

    public Task<IEnumerable<MonitoringSession>> Handle(GetSessionsQuery query) =>
        sessionRepository.FindByStatusAsync(query.Status);
}
