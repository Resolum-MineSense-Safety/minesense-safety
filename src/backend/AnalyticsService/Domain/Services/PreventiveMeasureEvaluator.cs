using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Domain.Services;

/// <summary>
/// Compares the alerts of the measure scope in equal windows before and after
/// it was applied (US25). Periods are comparable only when the "after" window
/// has fully elapsed and the "before" window has enough observations.
/// </summary>
public static class PreventiveMeasureEvaluator
{
    public static MeasureEvaluation Evaluate(PreventiveMeasure measure, IEnumerable<FatigueMetric> metrics,
        int windowDays, DateTimeOffset now)
    {
        if (windowDays is < 1 or > AnalyticsPolicy.MaxEvaluationWindowDays)
            throw new DomainException(
                $"The evaluation window must be between 1 and {AnalyticsPolicy.MaxEvaluationWindowDays} days.");

        var window = TimeSpan.FromDays(windowDays);
        var scoped = metrics.Where(metric => measure.Scope.Matches(metric.Context)).ToList();
        var before = Indicators(scoped, new AnalysisPeriod(measure.AppliedAt - window, measure.AppliedAt));
        var after = Indicators(scoped, new AnalysisPeriod(measure.AppliedAt, measure.AppliedAt + window));

        if (now < after.Period.To)
            return NotComparable(measure, before, after,
                $"The period after the measure is still running until {after.Period.To:u}; " +
                "both periods must have the same length before comparing them.");

        if (before.Alerts < AnalyticsPolicy.MinimumSampleSize)
            return NotComparable(measure, before, after,
                $"The period before the measure has only {before.Alerts} observations; at least " +
                $"{AnalyticsPolicy.MinimumSampleSize} are needed to compare.");

        var alertsVariation = Variation(before.AlertsPerDay, after.AlertsPerDay);
        var repeatedVariation = Variation(before.OperatorsWithRepeatedAlerts, after.OperatorsWithRepeatedAlerts);
        var trend = alertsVariation switch
        {
            < 0 => $"decreased {Math.Abs(alertsVariation.Value):0.##}%",
            > 0 => $"increased {alertsVariation.Value:0.##}%",
            _ => "did not change"
        };

        return new MeasureEvaluation(measure.Id, before, after, true, alertsVariation, repeatedVariation,
            $"Alerts per day {trend} after the measure. This is an observed variation; " +
            "it does not prove that accidents were avoided.");
    }

    private static PeriodIndicators Indicators(IEnumerable<FatigueMetric> metrics, AnalysisPeriod period)
    {
        var inPeriod = metrics.Where(metric => period.Contains(metric.OccurredAt)).ToList();
        return new PeriodIndicators(
            period,
            inPeriod.Count,
            inPeriod.Count(metric => metric.Severity == FatigueSeverity.Critical),
            Math.Round(inPeriod.Count / period.Length.TotalDays, 4),
            inPeriod.GroupBy(metric => metric.OperatorId).Count(group => group.Count() > 1));
    }

    private static double? Variation(double before, double after) =>
        before == 0 ? null : Math.Round((after - before) / before * 100, 2);

    private static MeasureEvaluation NotComparable(PreventiveMeasure measure, PeriodIndicators before,
        PeriodIndicators after, string reason) =>
        new(measure.Id, before, after, false, null, null, reason);
}
