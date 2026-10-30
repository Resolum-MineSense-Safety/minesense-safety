using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Services;

namespace AnalyticsService.Tests.Domain;

public class FatigueTrendAnalyzerTests
{
    private static readonly AnalysisPeriod Month = new(MetricFactory.Start, MetricFactory.Start.AddDays(30));

    [Fact]
    public void Analyze_SeveralShifts_ReportsFrequencyAndProportionPerShift()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(MetricFactory.Start, 15, "Night")
            .Concat(MetricFactory.Hourly(MetricFactory.Start, 10, "Day"))
            .Append(MetricFactory.Metric(MetricFactory.Start.AddDays(40), "Day")); // outside the period

        // Act
        var trend = FatigueTrendAnalyzer.Analyze(metrics, Month, AnalysisDimension.Shift);

        // Assert
        Assert.Equal(25, trend.TotalAlerts);
        Assert.True(trend.IsRepresentative);
        Assert.Null(trend.Warning);
        Assert.Equal(["Night", "Day"], trend.Groups.Select(group => group.Key));
        Assert.Equal(0.6, trend.Groups[0].Proportion);
        Assert.Equal(0.4, trend.Groups[1].Proportion);
    }

    [Fact]
    public void Analyze_GroupsIgnoringCaseAndCountsCriticalAlerts()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(MetricFactory.Start, 10, "Night").ToList();
        metrics.Add(MetricFactory.Metric(MetricFactory.Start, "NIGHT", severity: FatigueSeverity.Critical));

        // Act
        var trend = FatigueTrendAnalyzer.Analyze(metrics, Month, AnalysisDimension.Shift);

        // Assert
        var group = Assert.Single(trend.Groups);
        Assert.Equal(11, group.Alerts);
        Assert.Equal(1, group.CriticalAlerts);
    }

    [Fact]
    public void Analyze_SmallGroup_FlagsItAsNotRepresentative()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(MetricFactory.Start, 12, "Night")
            .Concat(MetricFactory.Hourly(MetricFactory.Start, 2, "Day"));

        // Act
        var trend = FatigueTrendAnalyzer.Analyze(metrics, Month, AnalysisDimension.Shift);

        // Assert
        Assert.True(trend.IsRepresentative);
        Assert.False(trend.Groups.Single(group => group.Key == "Day").IsRepresentative);
        Assert.Contains("indicative", trend.Warning);
    }

    [Fact]
    public void Analyze_InsufficientObservations_WarnsAndAvoidsConclusion()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(MetricFactory.Start, 3);

        // Act
        var trend = FatigueTrendAnalyzer.Analyze(metrics, Month, AnalysisDimension.Fleet);

        // Assert
        Assert.False(trend.IsRepresentative);
        Assert.Contains("no definitive conclusion", trend.Warning);
    }

    [Fact]
    public void Analyze_EmptyPeriod_ReturnsNoGroups()
    {
        // Act
        var trend = FatigueTrendAnalyzer.Analyze([], Month, AnalysisDimension.MineSite);

        // Assert
        Assert.Equal(0, trend.TotalAlerts);
        Assert.Empty(trend.Groups);
        Assert.False(trend.IsRepresentative);
    }
}
