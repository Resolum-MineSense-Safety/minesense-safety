using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace AnalyticsService.Tests.Infrastructure;

public class MongoRepositoriesTests
{
    private static readonly DateTimeOffset Start = MetricFactory.Start;

    private static PreventiveMeasure Measure() =>
        new(new RegisterPreventiveMeasureCommand("Night breaks", Enum.GetValues<PreventiveMeasureType>()[0],
            "Breaks every two hours", Start, new ContextFilter(Shift: "Night", Fleet: "CAT 797F"), Guid.NewGuid()));

    [Fact]
    public void Snapshot_RoundTrip_KeepsContextAndScope()
    {
        // Arrange
        var metric = MetricFactory.Metric(Start.AddTicks(3));
        var measure = Measure();

        // Act
        var restoredMetric = AggregateSnapshot.Deserialize<FatigueMetric>(AggregateSnapshot.Serialize(metric));
        var restoredMeasure = AggregateSnapshot.Deserialize<PreventiveMeasure>(AggregateSnapshot.Serialize(measure));

        // Assert
        Assert.Equal(metric.Context, restoredMetric.Context);
        Assert.Equal(metric.OccurredAt.Ticks, restoredMetric.OccurredAt.Ticks);
        Assert.Equal(measure.Scope, restoredMeasure.Scope);
        Assert.Equal(measure.Name, restoredMeasure.Name);
    }

    [MongoFact]
    public async Task Mongo_FatigueMetrics_FilterPeriodAndContextAndKeepOnePerAlert()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("analytics_tests");
        var repository = new MongoFatigueMetricRepository(mongo.Database);
        await repository.InitializeAsync(CancellationToken.None);
        var later = MetricFactory.Metric(Start.AddHours(2));
        var earlier = MetricFactory.Metric(Start);
        var edge = MetricFactory.Metric(Start.AddDays(1).AddTicks(-1));
        var period = new AnalysisPeriod(Start, Start.AddDays(1));

        // Act
        await repository.AddAsync(later);
        await repository.AddAsync(earlier);
        await repository.AddAsync(edge);
        await repository.AddAsync(MetricFactory.Metric(Start, fleet: "Komatsu 930E"));
        await repository.AddAsync(MetricFactory.Metric(Start.AddDays(1)));
        await repository.AddAsync(MetricFactory.Metric(Start.AddTicks(-1)));
        await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(
            new FatigueMetric(MetricFactory.Command(Start) with { AlertId = later.AlertId })));
        var restarted = new MongoFatigueMetricRepository(mongo.Reopen());

        // Assert
        Assert.Equal(new[] { earlier.Id, later.Id, edge.Id },
            (await restarted.FindAsync(period, new ContextFilter(Fleet: " cat 797f", Shift: "NIGHT"))).Select(item => item.Id));
        Assert.Empty(await restarted.FindAsync(period, new ContextFilter(MineSite: "Other")));
        Assert.Equal(later.Id, (await restarted.FindByAlertIdAsync(later.AlertId))!.Id);
    }

    [MongoFact]
    public async Task Mongo_OperatorContextsAndMeasures_SurviveRestartAndNewerWins()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("analytics_tests");
        var contexts = new MongoOperatorContextRepository(mongo.Database);
        var measures = new MongoPreventiveMeasureRepository(mongo.Database);
        await contexts.InitializeAsync(CancellationToken.None);
        await measures.InitializeAsync(CancellationToken.None);
        var operatorId = Guid.NewGuid();
        var current = new OperatorContext(operatorId, "Night", "Antamina", "Pit 2", "CAT 797F", Start.AddTicks(5));
        var measure = Measure();

        // Act
        await contexts.UpsertAsync(current);
        await contexts.UpsertAsync(current with { Fleet = "Stale", UpdatedAt = Start.AddTicks(4) });
        await contexts.UpsertAsync(current with { Location = "Pit 3", UpdatedAt = Start.AddTicks(5) });
        await measures.AddAsync(measure);
        var restartedContexts = new MongoOperatorContextRepository(mongo.Reopen());
        var restartedMeasures = new MongoPreventiveMeasureRepository(mongo.Reopen());

        // Assert
        Assert.Equal(current with { Location = "Pit 3" }, await restartedContexts.FindByOperatorIdAsync(operatorId));
        Assert.Null(await restartedContexts.FindByOperatorIdAsync(Guid.NewGuid()));
        Assert.Equal(measure.Scope, Assert.Single(await restartedMeasures.ListAsync()).Scope);
    }
}
