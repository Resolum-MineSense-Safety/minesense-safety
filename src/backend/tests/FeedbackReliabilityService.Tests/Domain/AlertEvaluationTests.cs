using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Tests.Domain;

public class AlertEvaluationTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);
    private static AlertEvaluation Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

    [Theory]
    [InlineData(FeedbackClassification.Confirmed)]
    [InlineData(FeedbackClassification.FalseAlarm)]
    [InlineData(FeedbackClassification.Inconclusive)]
    public void Report_ValidClassification_PreservesOriginalAndRecordsFeedback(FeedbackClassification classification)
    {
        // Arrange
        var original = Create();
        var reporter = original.OperatorId;

        // Act
        var updated = original.Report(reporter, classification, "  field observation  ", Now.AddSeconds(1));

        // Assert
        Assert.Empty(original.Feedback);
        Assert.Equal(classification, updated.EffectiveClassification);
        Assert.Equal("AWAITING_REVIEW", updated.Status);
        Assert.Equal("field observation", Assert.Single(updated.Feedback).Reason);
        Assert.Equal(1, updated.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Report_FalseAlarmWithoutReason_RejectsIt(string? reason)
    {
        // Arrange
        var alert = Create();

        // Act
        var action = () => alert.Report(alert.OperatorId, FeedbackClassification.FalseAlarm, reason, Now);

        // Assert
        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Report_SameUserTwice_RejectsDuplicate()
    {
        // Arrange
        var alert = Create();
        var evaluated = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now);

        // Act
        var action = () => evaluated.Report(alert.OperatorId, FeedbackClassification.Inconclusive, null, Now);

        // Assert
        Assert.Throws<DomainConflictException>(action);
    }

    [Fact]
    public void Review_CorrectsClassificationAndPreservesBothHistories()
    {
        // Arrange
        var alert = Create();
        var reported = alert.Report(alert.OperatorId, FeedbackClassification.FalseAlarm, "Dust on lens", Now);

        // Act
        var reviewed = reported.Review(Guid.NewGuid(), FeedbackClassification.Inconclusive, "Needs evidence", Now.AddMinutes(1));
        var corrected = reviewed.Review(Guid.NewGuid(), FeedbackClassification.Confirmed, "Evidence confirmed", Now.AddMinutes(2));

        // Assert
        Assert.Equal(FeedbackClassification.Confirmed, corrected.EffectiveClassification);
        Assert.Equal(FeedbackClassification.FalseAlarm, Assert.Single(corrected.Feedback).Classification);
        Assert.Equal(2, corrected.Reviews.Count);
        Assert.Equal("REVIEWED", corrected.Status);
        Assert.Single(reviewed.Reviews);
    }

    [Fact]
    public void Review_UnclassifiedAlert_RemainsPending()
    {
        // Arrange
        var alert = Create();

        // Act
        var action = () => alert.Review(Guid.NewGuid(), FeedbackClassification.Confirmed, "Checked", Now);

        // Assert
        Assert.Throws<DomainConflictException>(action);
        Assert.Equal("PENDING", alert.Status);
        Assert.Null(alert.EffectiveClassification);
    }

    [Fact]
    public void Report_InvalidClassification_RejectsUndefinedEnum()
    {
        // Arrange
        var alert = Create();

        // Act
        var action = () => alert.Report(alert.OperatorId, (FeedbackClassification)99, null, Now);

        // Assert
        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Report_BeforeAlert_RejectsTimestamp()
    {
        // Arrange
        var alert = Create();

        // Act
        var action = () => alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now.AddTicks(-1));

        // Assert
        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Review_BeforeFeedback_RejectsTimestamp()
    {
        // Arrange
        var alert = Create();
        var reported = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now.AddMinutes(1));

        // Act
        var action = () => reported.Review(Guid.NewGuid(), FeedbackClassification.Confirmed, "Checked", Now);

        // Assert
        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Review_MissingObservation_RejectsIt(string observation)
    {
        // Arrange
        var alert = Create();
        var reported = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now);

        // Act
        var action = () => reported.Review(Guid.NewGuid(), FeedbackClassification.Confirmed, observation, Now);

        // Assert
        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Report_ReviewedAlert_RejectsLateFeedback()
    {
        // Arrange
        var alert = Create();
        var reviewed = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now)
            .Review(Guid.NewGuid(), FeedbackClassification.Confirmed, "Checked", Now);

        // Act
        var action = () => reviewed.Report(Guid.NewGuid(), FeedbackClassification.Inconclusive, null, Now);

        // Assert
        Assert.Throws<DomainConflictException>(action);
    }
}
