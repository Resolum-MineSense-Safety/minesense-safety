using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Interfaces.REST.Resources;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class MonitoredOperatorResourceFromEntityAssembler
{
    public static MonitoredOperatorResource ToResourceFromEntity(MonitoredOperator monitoredOperator) =>
        new(monitoredOperator.Id, monitoredOperator.OperatorId, monitoredOperator.FullName,
            monitoredOperator.VehicleCode, monitoredOperator.Fleet, monitoredOperator.Shift.ToString(),
            monitoredOperator.Location, monitoredOperator.CurrentRiskLevel.ToString(),
            monitoredOperator.LastUpdatedAt);
}
