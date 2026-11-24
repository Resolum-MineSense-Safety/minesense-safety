using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Interfaces.REST.Resources;

namespace AnalyticsService.Interfaces.REST.Transform;

public static class MeasureEvaluationResourceFromReadModelAssembler
{
    public static MeasureEvaluationResource ToResourceFromReadModel(MeasureEvaluation evaluation) =>
        new(evaluation.MeasureId, ToResource(evaluation.Before), ToResource(evaluation.After),
            evaluation.IsComparable, evaluation.AlertsPerDayVariationPercent,
            evaluation.RepeatedAlertsVariationPercent, evaluation.Conclusion);

    private static PeriodIndicatorsResource ToResource(PeriodIndicators indicators) =>
        new(indicators.Period.From, indicators.Period.To, indicators.Alerts, indicators.CriticalAlerts,
            indicators.AlertsPerDay, indicators.OperatorsWithRepeatedAlerts);
}
