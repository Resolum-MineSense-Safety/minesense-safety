using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Model.ReadModels;

/// <summary>Indicators of one side of the comparison.</summary>
public record PeriodIndicators(
    AnalysisPeriod Period,
    int Alerts,
    int CriticalAlerts,
    double AlertsPerDay,
    int OperatorsWithRepeatedAlerts);

/// <summary>
/// Before/after comparison of US25. Variations are observations, never a claim
/// that accidents were avoided.
/// </summary>
public record MeasureEvaluation(
    Guid MeasureId,
    PeriodIndicators Before,
    PeriodIndicators After,
    bool IsComparable,
    double? AlertsPerDayVariationPercent,
    double? RepeatedAlertsVariationPercent,
    string Conclusion);
