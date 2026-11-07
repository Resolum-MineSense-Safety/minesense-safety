using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;

namespace AnalyticsService.Domain.Services;

public interface IFatigueMetricCommandService
{
    Task<FatigueMetric> Handle(RecordFatigueMetricCommand command);
}
