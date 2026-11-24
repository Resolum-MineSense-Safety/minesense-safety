namespace AnalyticsService.Interfaces.REST.Resources;

public record PeriodIndicatorsResource(
    DateTimeOffset From,
    DateTimeOffset To,
    int Alerts,
    int CriticalAlerts,
    double AlertsPerDay,
    int OperatorsWithRepeatedAlerts);

public record MeasureEvaluationResource(
    Guid MeasureId,
    PeriodIndicatorsResource Before,
    PeriodIndicatorsResource After,
    bool IsComparable,
    double? AlertsPerDayVariationPercent,
    double? RepeatedAlertsVariationPercent,
    string Conclusion);
