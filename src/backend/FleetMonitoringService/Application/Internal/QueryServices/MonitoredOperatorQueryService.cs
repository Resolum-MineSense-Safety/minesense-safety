using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Queries;
using FleetMonitoringService.Domain.Repositories;
using FleetMonitoringService.Domain.Services;

namespace FleetMonitoringService.Application.Internal.QueryServices;

public class MonitoredOperatorQueryService(IMonitoredOperatorRepository monitoredOperatorRepository)
    : IMonitoredOperatorQueryService
{
    public Task<MonitoredOperator?> Handle(GetMonitoredOperatorByOperatorIdQuery query) =>
        monitoredOperatorRepository.FindByOperatorIdAsync(query.OperatorId);

    public Task<IEnumerable<MonitoredOperator>> Handle(GetFleetStatusQuery query) =>
        monitoredOperatorRepository.FindByCriteriaAsync(query.Shift, query.Fleet, query.Location, query.RiskLevel);
}
