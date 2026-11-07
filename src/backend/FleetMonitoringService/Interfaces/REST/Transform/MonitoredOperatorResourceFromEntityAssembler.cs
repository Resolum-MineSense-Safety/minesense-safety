using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Domain.Services;
using FleetMonitoringService.Interfaces.REST.Resources;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class MonitoredOperatorResourceFromEntityAssembler
{
    public static MonitoredOperatorResource ToResourceFromEntity(
        MonitoredOperator monitoredOperator,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        var freshness = FleetStatusFreshnessPolicy.Evaluate(monitoredOperator.LastUpdatedAt, now, staleAfter);

        return new(monitoredOperator.Id, monitoredOperator.OperatorId, monitoredOperator.FullName,
            monitoredOperator.VehicleCode, monitoredOperator.Fleet,
            monitoredOperator.MiningUnitId, monitoredOperator.FleetId, monitoredOperator.Shift.ToString(),
            monitoredOperator.Location, monitoredOperator.CurrentRiskLevel.ToString(),
            monitoredOperator.LastUpdatedAt, freshness.ToString(), freshness == FreshnessStatus.Stale);
    }
}
