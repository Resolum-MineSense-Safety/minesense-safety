using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Services;

/// <summary>Groups fatigue alerts by an operational dimension (US24, TS-22).</summary>
public static class FatigueTrendAnalyzer
{
    public static FatigueTrend Analyze(IEnumerable<FatigueMetric> metrics, AnalysisPeriod period,
        AnalysisDimension groupBy)
    {
        var inPeriod = metrics.Where(metric => period.Contains(metric.OccurredAt)).ToList();
        var total = inPeriod.Count;

        var groups = inPeriod
            .GroupBy(metric => metric.Context.ValueOf(groupBy), StringComparer.OrdinalIgnoreCase)
            .Select(group => new TrendGroup(
                group.Key,
                group.Count(),
                group.Count(metric => metric.Severity == FatigueSeverity.Critical),
                Math.Round((double)group.Count() / total, 4),
                group.Count() >= AnalyticsPolicy.MinimumSampleSize))
            .OrderByDescending(group => group.Alerts)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var isRepresentative = total >= AnalyticsPolicy.MinimumSampleSize;
        string? warning = null;
        if (!isRepresentative)
            warning = $"Only {total} observations in the period; at least {AnalyticsPolicy.MinimumSampleSize} " +
                      "are needed, so no definitive conclusion should be drawn.";
        else if (groups.Any(group => !group.IsRepresentative))
            warning = "Some groups have fewer than " + AnalyticsPolicy.MinimumSampleSize +
                      " observations; their proportions are indicative only.";

        return new FatigueTrend(period, groupBy, total, groups, isRepresentative, warning);
    }
}
