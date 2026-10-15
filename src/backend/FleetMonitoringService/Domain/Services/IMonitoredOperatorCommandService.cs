using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Commands;

namespace FleetMonitoringService.Domain.Services;

public interface IMonitoredOperatorCommandService
{
    Task<MonitoredOperator> Handle(RegisterMonitoredOperatorCommand command);
    Task<MonitoredOperator?> Handle(UpdateOperatorRiskLevelCommand command);
    Task<MonitoredOperator?> Handle(ReassignOperatorVehicleCommand command);
}
