using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Tests.Domain;

public class PreventiveMeasureEvaluatorTests
{
    private static readonly DateTimeOffset AppliedAt = MetricFactory.Start.AddDays(30);
    private static readonly DateTimeOffset AfterWindows = AppliedAt.AddDays(31);

    private static PreventiveMeasure Measure(ContextFilter? scope = null) =>
        new(new RegisterPreventiveMeasureCommand("Night pause", PreventiveMeasureType.PreventivePause, null,
            AppliedAt, scope ?? new ContextFilter(Shift: "Night"), Guid.NewGuid()));

    [Fact]
    public void Evaluate_FewerAlertsAfterMeasure_ReportsDecreaseWithoutClaimingAvoidedAccidents()
    {
        // Arrange
        var repeatOperator = Guid.NewGuid();
        var metrics = MetricFactory.Hourly(AppliedAt.AddDays(-10), 20, operatorId: repeatOperator)
            .Concat(MetricFactory.Hourly(AppliedAt.AddDays(5), 10))
            .Concat(MetricFactory.Hourly(AppliedAt.AddDays(-10), 30, "Day")); // outside the scope

        // Act
        var evaluation = PreventiveMeasureEvaluator.Evaluate(Measure(), metrics, 30, AfterWindows);

        // Assert
        Assert.True(evaluation.IsComparable);
        Assert.Equal(20, evaluation.Before.Alerts);
        Assert.Equal(10, evaluation.After.Alerts);
        Assert.Equal(-50, evaluation.AlertsPerDayVariationPercent);
        Assert.Equal(-100, evaluation.RepeatedAlertsVariationPercent);
        Assert.Contains("decreased 50%", evaluation.Conclusion);
        Assert.Contains("does not prove that accidents were avoided", evaluation.Conclusion);
    }

    [Fact]
    public void Evaluate_MoreAlertsAfterMeasure_ReportsIncrease()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(AppliedAt.AddDays(-3), 10)
            .Concat(MetricFactory.Hourly(AppliedAt.AddDays(3), 15));

        // Act
        var evaluation = PreventiveMeasureEvaluator.Evaluate(Measure(), metrics, 30, AfterWindows);

        // Assert
        Assert.Equal(50, evaluation.AlertsPerDayVariationPercent);
        Assert.Null(evaluation.RepeatedAlertsVariationPercent);
        Assert.Contains("increased 50%", evaluation.Conclusion);
    }

    [Fact]
    public void Evaluate_SameAlertsBeforeAndAfter_ReportsNoChange()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(AppliedAt.AddDays(-3), 10)
            .Concat(MetricFactory.Hourly(AppliedAt.AddDays(3), 10));

        // Act
        var evaluation = PreventiveMeasureEvaluator.Evaluate(Measure(), metrics, 30, AfterWindows);

        // Assert
        Assert.Equal(0, evaluation.AlertsPerDayVariationPercent);
        Assert.Contains("did not change", evaluation.Conclusion);
    }

    [Fact]
    public void Evaluate_AfterWindowStillRunning_IsNotComparable()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(AppliedAt.AddDays(-3), 20);

        // Act
        var evaluation = PreventiveMeasureEvaluator.Evaluate(Measure(), metrics, 30, AppliedAt.AddDays(10));

        // Assert
        Assert.False(evaluation.IsComparable);
        Assert.Null(evaluation.AlertsPerDayVariationPercent);
        Assert.Contains("still running", evaluation.Conclusion);
    }

    [Fact]
    public void Evaluate_InsufficientDataBefore_IsNotComparable()
    {
        // Arrange
        var metrics = MetricFactory.Hourly(AppliedAt.AddDays(-3), 4)
            .Concat(MetricFactory.Hourly(AppliedAt.AddDays(3), 2));

        // Act
        var evaluation = PreventiveMeasureEvaluator.Evaluate(Measure(), metrics, 30, AfterWindows);

        // Assert
        Assert.False(evaluation.IsComparable);
        Assert.Contains("only 4 observations", evaluation.Conclusion);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(181)]
    public void Evaluate_InvalidWindow_Throws(int windowDays)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => PreventiveMeasureEvaluator.Evaluate(Measure(), [], windowDays, AfterWindows));
    }
}
