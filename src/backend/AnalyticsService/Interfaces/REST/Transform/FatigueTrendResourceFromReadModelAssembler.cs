using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Interfaces.REST.Resources;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class FatigueTrendResourceFromReadModelAssembler
{
    public static FatigueTrendResource ToResourceFromReadModel(FatigueTrend trend) =>
        new(trend.Period.From, trend.Period.To, trend.GroupBy.ToString(), trend.TotalAlerts,
            trend.IsRepresentative, trend.Warning,
            trend.Groups.Select(group => new TrendGroupResource(group.Key, group.Alerts, group.CriticalAlerts,
                group.Proportion, group.IsRepresentative)).ToList());
}
