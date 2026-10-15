namespace AnalyticsService.Domain.Services;

/// <summary>Agreed thresholds of the analytical model (EP11).</summary>
public static class AnalyticsPolicy
{
    /// <summary>Observations a group or period needs before it supports a conclusion.</summary>
    public const int MinimumSampleSize = 10;

    public const int DefaultEvaluationWindowDays = 30;
    public const int MaxEvaluationWindowDays = 180;
}
