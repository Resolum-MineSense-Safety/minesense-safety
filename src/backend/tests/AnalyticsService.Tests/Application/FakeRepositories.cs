using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Repositories;

namespace AnalyticsService.Tests.Application;

/// <summary>Repository doubles so the application layer is tested without Infrastructure.</summary>
internal class FakeFatigueMetricRepository : IFatigueMetricRepository
{
    public List<FatigueMetric> Items { get; } = [];

    public Task AddAsync(FatigueMetric aggregate)
    {
        Items.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<FatigueMetric?> FindByIdAsync(Guid id) => Task.FromResult(Items.Find(item => item.Id == id));

    public Task<IEnumerable<FatigueMetric>> ListAsync() => Task.FromResult<IEnumerable<FatigueMetric>>(Items);

    public Task UpdateAsync(FatigueMetric aggregate) => Task.CompletedTask;

    public Task<IEnumerable<FatigueMetric>> FindAsync(AnalysisPeriod period, ContextFilter filter) =>
        Task.FromResult<IEnumerable<FatigueMetric>>(Items
            .Where(item => period.Contains(item.OccurredAt) && filter.Matches(item.Context)).ToList());

    public Task<FatigueMetric?> FindByAlertIdAsync(Guid alertId) =>
        Task.FromResult(Items.Find(item => item.AlertId == alertId));
}

internal class FakePreventiveMeasureRepository : IPreventiveMeasureRepository
{
    public List<PreventiveMeasure> Items { get; } = [];

    public Task AddAsync(PreventiveMeasure aggregate)
    {
        Items.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<PreventiveMeasure?> FindByIdAsync(Guid id) => Task.FromResult(Items.Find(item => item.Id == id));

    public Task<IEnumerable<PreventiveMeasure>> ListAsync() => Task.FromResult<IEnumerable<PreventiveMeasure>>(Items);

    public Task UpdateAsync(PreventiveMeasure aggregate) => Task.CompletedTask;
}

/// <summary>Clock the tests can move forward.</summary>
internal class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
