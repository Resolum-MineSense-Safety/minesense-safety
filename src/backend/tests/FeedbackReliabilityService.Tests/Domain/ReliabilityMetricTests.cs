using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Tests.Domain;

public class ReliabilityMetricTests
{
    private static readonly DateTimeOffset From = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_UsesOneFinalClassificationPerAlertAndExcludesPendingFromDenominator()
    {
        // Arrange
        var operation = Guid.NewGuid();
        var query = new EvaluationQuery(operation, From, From.AddDays(1));
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), operation, From);
        var first = alert.Report(alert.OperatorId, FeedbackClassification.FalseAlarm, "Incorrect", From)
            .Review(Guid.NewGuid(), FeedbackClassification.Confirmed, "Confirmed by evidence", From);
        var second = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), operation, From)
            .Report(Guid.NewGuid(), FeedbackClassification.Inconclusive, null, From);
        var pending = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), operation, From);

        // Act
        var metric = ReliabilityMetric.Calculate(query, [first, second, pending], 3);

        // Assert
        Assert.Equal(3, metric.TotalAlerts);
        Assert.Equal(2, metric.SampleSize);
        Assert.Equal(1, metric.PendingAlerts);
        Assert.Equal(0, metric.FalseAlarms);
        Assert.Equal(0.5, metric.ConfirmedProportion);
        Assert.Equal(0.5, metric.InconclusiveProportion);
        Assert.True(metric.InsufficientSample);
    }

    [Fact]
    public void Calculate_EmptySample_ReturnsZerosWithoutDivisionByZero()
    {
        // Arrange
        var query = new EvaluationQuery(Guid.NewGuid(), From, From.AddDays(1));

        // Act
        var metric = ReliabilityMetric.Calculate(query, [], 30);

        // Assert
        Assert.Equal(0, metric.SampleSize);
        Assert.Equal(0, metric.ConfirmedProportion + metric.FalseAlarmProportion + metric.InconclusiveProportion);
        Assert.True(metric.InsufficientSample);
    }

    [Fact]
    public void Calculate_PeriodAndOperation_AreAppliedConsistently()
    {
        // Arrange
        var operation = Guid.NewGuid();
        var query = new EvaluationQuery(operation, From, From.AddDays(1));
        AlertEvaluation Evaluated(Guid scope, DateTimeOffset issuedAt) => new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), scope, issuedAt)
            .Report(Guid.NewGuid(), FeedbackClassification.FalseAlarm, "No fatigue", issuedAt);
        var alerts = new[] { Evaluated(operation, From), Evaluated(operation, query.To),
            Evaluated(operation, From.AddTicks(-1)), Evaluated(Guid.NewGuid(), From) };

        // Act
        var metric = ReliabilityMetric.Calculate(query, alerts, 1);

        // Assert
        Assert.Equal(1, metric.TotalAlerts);
        Assert.Equal(1, metric.FalseAlarmProportion);
        Assert.False(metric.InsufficientSample);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(367)]
    public void Query_InvalidPeriod_RejectsIt(int days)
    {
        // Arrange
        var query = new EvaluationQuery(Guid.NewGuid(), From, From.AddDays(days));

        // Act
        var action = query.Validate;

        // Assert
        Assert.Throws<DomainException>(action);
    }
}
