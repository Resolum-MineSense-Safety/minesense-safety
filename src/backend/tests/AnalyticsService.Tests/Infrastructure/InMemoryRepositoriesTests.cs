using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Infrastructure.Persistence.InMemory;

namespace AnalyticsService.Tests.Infrastructure;

public class InMemoryRepositoriesTests
{
    private readonly InMemoryFatigueMetricRepository _repository = new();

    [Fact]
    public async Task FindAsync_ReturnsMetricsInPeriodAndScopeOldestFirst()
    {
        // Arrange
        var later = MetricFactory.Metric(MetricFactory.Start.AddHours(2));
        var earlier = MetricFactory.Metric(MetricFactory.Start);
        await _repository.AddAsync(later);
        await _repository.AddAsync(earlier);
        await _repository.AddAsync(MetricFactory.Metric(MetricFactory.Start, fleet: "Komatsu 930E"));
        await _repository.AddAsync(MetricFactory.Metric(MetricFactory.Start.AddDays(3)));
        var period = new AnalysisPeriod(MetricFactory.Start, MetricFactory.Start.AddDays(1));

        // Act
        var result = await _repository.FindAsync(period, new ContextFilter(Fleet: "CAT 797F"));

        // Assert
        Assert.Equal([earlier, later], result);
    }

    [Fact]
    public async Task FindByAlertIdAsync_ReturnsMetricOfThatAlert()
    {
        // Arrange
        var metric = MetricFactory.Metric(MetricFactory.Start);
        await _repository.AddAsync(metric);

        // Act & Assert
        Assert.Same(metric, await _repository.FindByAlertIdAsync(metric.AlertId));
        Assert.Null(await _repository.FindByAlertIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task PreventiveMeasureRepository_StoresMeasures()
    {
        // Arrange
        var repository = new InMemoryPreventiveMeasureRepository();
        var measure = new PreventiveMeasure(
            new RegisterPreventiveMeasureCommand("Relief", PreventiveMeasureType.OperatorRelief,
                null, MetricFactory.Start, ContextFilter.All, Guid.NewGuid()));

        // Act
        await repository.AddAsync(measure);

        // Assert
        Assert.Same(measure, await repository.FindByIdAsync(measure.Id));
    }
}
