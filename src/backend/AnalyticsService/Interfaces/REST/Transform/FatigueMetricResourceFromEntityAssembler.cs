using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Interfaces.REST.Resources;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class FatigueMetricResourceFromEntityAssembler
{
    public static FatigueMetricResource ToResourceFromEntity(FatigueMetric metric) =>
        new(metric.Id, metric.OperatorId, metric.AlertId, metric.Severity.ToString(), metric.OccurredAt,
            metric.Context.Shift, metric.Context.MineSite, metric.Context.Location, metric.Context.Fleet);
}
