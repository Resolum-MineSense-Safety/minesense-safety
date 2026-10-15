using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Queries;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;

namespace AnalyticsService.Application.Internal.QueryServices;

public class AnalyticsQueryService(
    IFatigueMetricRepository fatigueMetricRepository,
    IPreventiveMeasureRepository preventiveMeasureRepository,
    TimeProvider timeProvider) : IAnalyticsQueryService
{
    public async Task<FatigueTrend> Handle(GetFatigueTrendsQuery query)
    {
        var metrics = await fatigueMetricRepository.FindAsync(query.Period, query.Filter);
        return FatigueTrendAnalyzer.Analyze(metrics, query.Period, query.GroupBy);
    }

    public Task<PreventiveMeasure?> Handle(GetPreventiveMeasureByIdQuery query) =>
        preventiveMeasureRepository.FindByIdAsync(query.MeasureId);

    public async Task<IEnumerable<PreventiveMeasure>> Handle(GetAllPreventiveMeasuresQuery query) =>
        (await preventiveMeasureRepository.ListAsync()).OrderByDescending(measure => measure.AppliedAt);

    public async Task<MeasureEvaluation?> Handle(EvaluatePreventiveMeasureQuery query)
    {
        var measure = await preventiveMeasureRepository.FindByIdAsync(query.MeasureId);
        if (measure is null) return null;

        var window = TimeSpan.FromDays(Math.Clamp(query.WindowDays, 1, AnalyticsPolicy.MaxEvaluationWindowDays));
        var span = new AnalysisPeriod(measure.AppliedAt - window, measure.AppliedAt + window);
        var metrics = await fatigueMetricRepository.FindAsync(span, measure.Scope);
        return PreventiveMeasureEvaluator.Evaluate(measure, metrics, query.WindowDays, timeProvider.GetUtcNow());
    }
}
