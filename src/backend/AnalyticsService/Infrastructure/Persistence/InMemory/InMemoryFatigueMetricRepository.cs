using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace AnalyticsService.Infrastructure.Persistence.InMemory;

public class InMemoryFatigueMetricRepository : InMemoryRepository<FatigueMetric>, IFatigueMetricRepository
{
    public Task<IEnumerable<FatigueMetric>> FindAsync(AnalysisPeriod period, ContextFilter filter) =>
        Task.FromResult<IEnumerable<FatigueMetric>>(
            Store.Values
                .Where(metric => period.Contains(metric.OccurredAt) && filter.Matches(metric.Context))
                .OrderBy(metric => metric.OccurredAt)
                .ToList());

    public Task<FatigueMetric?> FindByAlertIdAsync(Guid alertId) =>
        Task.FromResult(Store.Values.FirstOrDefault(metric => metric.AlertId == alertId));
}
