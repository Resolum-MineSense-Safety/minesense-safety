using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Queries;
using AnalyticsService.Domain.Model.ReadModels;

namespace AnalyticsService.Domain.Services;

public interface IAnalyticsQueryService
{
    Task<FatigueTrend> Handle(GetFatigueTrendsQuery query);
    Task<PreventiveMeasure?> Handle(GetPreventiveMeasureByIdQuery query);
    Task<IEnumerable<PreventiveMeasure>> Handle(GetAllPreventiveMeasuresQuery query);

    /// <summary>Null when the measure does not exist.</summary>
    Task<MeasureEvaluation?> Handle(EvaluatePreventiveMeasureQuery query);
}
