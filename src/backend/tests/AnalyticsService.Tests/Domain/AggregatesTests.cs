using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Tests.Domain;

public class AggregatesTests
{
    [Fact]
    public void FatigueMetric_WithValidCommand_KeepsContext()
    {
        // Act
        var metric = MetricFactory.Metric(MetricFactory.Start, shift: "Day");

        // Assert
        Assert.Equal("Day", metric.Context.Shift);
        Assert.Equal(MetricFactory.Start, metric.OccurredAt);
    }

    [Fact]
    public void FatigueMetric_WithoutOperatorOrAlert_Throws()
    {
        // Arrange
        var noOperator = MetricFactory.Command(MetricFactory.Start) with { OperatorId = Guid.Empty };
        var noAlert = MetricFactory.Command(MetricFactory.Start) with { AlertId = Guid.Empty };

        // Act & Assert
        Assert.Throws<DomainException>(() => new FatigueMetric(noOperator));
        Assert.Throws<DomainException>(() => new FatigueMetric(noAlert));
    }

    [Fact]
    public void PreventiveMeasure_WithValidCommand_TrimsNameAndKeepsScope()
    {
        // Arrange
        var scope = new ContextFilter(Shift: "Night");

        // Act
        var measure = new PreventiveMeasure(new RegisterPreventiveMeasureCommand(" Night pause ",
            PreventiveMeasureType.PreventivePause, null, MetricFactory.Start, scope, Guid.NewGuid()));

        // Assert
        Assert.Equal("Night pause", measure.Name);
        Assert.Equal(string.Empty, measure.Description);
        Assert.Same(scope, measure.Scope);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("Pause", true)]
    public void PreventiveMeasure_WithoutNameOrRegistrant_Throws(string name, bool emptyRegistrant)
    {
        // Arrange
        var command = new RegisterPreventiveMeasureCommand(name, PreventiveMeasureType.Other, "x",
            MetricFactory.Start, ContextFilter.All, emptyRegistrant ? Guid.Empty : Guid.NewGuid());

        // Act & Assert
        Assert.Throws<DomainException>(() => new PreventiveMeasure(command));
    }
}
