using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Repositories;

namespace AnalyticsService.Domain.Repositories;

public interface IFatigueMetricRepository : IBaseRepository<FatigueMetric>
{
    /// <summary>Metrics inside the period that match the filter, oldest first.</summary>
    Task<IEnumerable<FatigueMetric>> FindAsync(AnalysisPeriod period, ContextFilter filter);

    Task<FatigueMetric?> FindByAlertIdAsync(Guid alertId);
}
