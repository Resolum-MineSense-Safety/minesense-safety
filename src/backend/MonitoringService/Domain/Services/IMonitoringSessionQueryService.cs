using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Queries;

namespace MonitoringService.Domain.Services;

public interface IMonitoringSessionQueryService
{
    Task<MonitoringSession?> Handle(GetSessionByIdQuery query);
    Task<MonitoringSession?> Handle(GetCurrentStatusByOperatorQuery query);
    Task<IEnumerable<MonitoringSession>> Handle(GetSessionsQuery query);
}
