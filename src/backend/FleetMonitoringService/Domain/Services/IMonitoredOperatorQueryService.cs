using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Queries;

namespace FleetMonitoringService.Domain.Services;

public interface IMonitoredOperatorQueryService
{
    Task<MonitoredOperator?> Handle(GetMonitoredOperatorByOperatorIdQuery query);
    Task<IEnumerable<MonitoredOperator>> Handle(GetFleetStatusQuery query);
}
