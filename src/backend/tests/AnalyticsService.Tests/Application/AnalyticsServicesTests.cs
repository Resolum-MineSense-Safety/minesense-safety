using AnalyticsService.Application.Internal.CommandServices;
using AnalyticsService.Application.Internal.QueryServices;
using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.Queries;
using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Tests.Application;

public class AnalyticsServicesTests
{
    private readonly FakeFatigueMetricRepository _metrics = new();
    private readonly FakePreventiveMeasureRepository _measures = new();
    private readonly FakeTimeProvider _clock = new(MetricFactory.Start.AddDays(90));
    private readonly FatigueMetricCommandService _metricCommandService;
    private readonly PreventiveMeasureCommandService _measureCommandService;
    private readonly AnalyticsQueryService _queryService;

    public AnalyticsServicesTests()
    {
        _metricCommandService = new FatigueMetricCommandService(_metrics);
        _measureCommandService = new PreventiveMeasureCommandService(_measures);
        _queryService = new AnalyticsQueryService(_metrics, _measures, _clock);
    }

    private Task<PreventiveMeasure> RegisterMeasureAsync(DateTimeOffset appliedAt) =>
        _measureCommandService.Handle(new RegisterPreventiveMeasureCommand("Night pause",
            PreventiveMeasureType.PreventivePause, null, appliedAt, new ContextFilter(Shift: "Night"), Guid.NewGuid()));

    [Fact]
    public async Task Handle_RecordFatigueMetric_IsIdempotentPerAlert()
    {
        // Arrange
        var command = MetricFactory.Command(MetricFactory.Start);

        // Act
        var first = await _metricCommandService.Handle(command);
        var second = await _metricCommandService.Handle(command);

        // Assert
        Assert.Same(first, second);
        Assert.Single(_metrics.Items);
    }

    [Fact]
    public async Task Handle_GetFatigueTrends_AppliesFilterAndPeriod()
    {
        // Arrange
        foreach (var command in Enumerable.Range(0, 12).Select(h => MetricFactory.Command(MetricFactory.Start.AddHours(h))))
            await _metricCommandService.Handle(command);
        await _metricCommandService.Handle(MetricFactory.Command(MetricFactory.Start, mineSite: "Cerro Verde"));
        var period = new AnalysisPeriod(MetricFactory.Start, MetricFactory.Start.AddDays(1));

        // Act
        var trend = await _queryService.Handle(new GetFatigueTrendsQuery(period, AnalysisDimension.MineSite,
            new ContextFilter(MineSite: "Antamina")));

        // Assert
        Assert.Equal(12, trend.TotalAlerts);
        Assert.Equal("Antamina", Assert.Single(trend.Groups).Key);
    }

    [Fact]
    public async Task Handle_PreventiveMeasureQueries_ReturnRegisteredMeasuresNewestFirst()
    {
        // Arrange
        var older = await RegisterMeasureAsync(MetricFactory.Start);
        var newer = await RegisterMeasureAsync(MetricFactory.Start.AddDays(10));

        // Act
        var all = await _queryService.Handle(new GetAllPreventiveMeasuresQuery());
        var byId = await _queryService.Handle(new GetPreventiveMeasureByIdQuery(older.Id));

        // Assert
        Assert.Equal([newer, older], all);
        Assert.Same(older, byId);
    }

    [Fact]
    public async Task Handle_EvaluatePreventiveMeasure_ComparesScopedMetricsAroundMeasure()
    {
        // Arrange
        var appliedAt = MetricFactory.Start.AddDays(30);
        var measure = await RegisterMeasureAsync(appliedAt);
        foreach (var metric in MetricFactory.Hourly(appliedAt.AddDays(-5), 20)
                     .Concat(MetricFactory.Hourly(appliedAt.AddDays(5), 5)))
            await _metrics.AddAsync(metric);

        // Act
        var evaluation = await _queryService.Handle(new EvaluatePreventiveMeasureQuery(measure.Id, 30));

        // Assert
        Assert.True(evaluation!.IsComparable);
        Assert.Equal(-75, evaluation.AlertsPerDayVariationPercent);
    }

    [Fact]
    public async Task Handle_EvaluateUnknownMeasure_ReturnsNull()
    {
        // Act & Assert
        Assert.Null(await _queryService.Handle(new EvaluatePreventiveMeasureQuery(Guid.NewGuid(), 30)));
    }
}
