using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Commands;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using FeedbackReliabilityService.Interfaces.REST.Resources;

namespace FeedbackReliabilityService.Interfaces.REST.Transform;

public static class FeedbackAssembler
{
    public static FeedbackClassification Classification(string value) => value switch
    {
        "CONFIRMED" => FeedbackClassification.Confirmed,
        "FALSE_ALARM" => FeedbackClassification.FalseAlarm,
        "INCONCLUSIVE" => FeedbackClassification.Inconclusive,
        _ => throw new ArgumentException("Invalid classification.", nameof(value))
    };

    public static string Classification(FeedbackClassification value) => value switch
    {
        FeedbackClassification.Confirmed => "CONFIRMED",
        FeedbackClassification.FalseAlarm => "FALSE_ALARM",
        FeedbackClassification.Inconclusive => "INCONCLUSIVE",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    public static ReportFeedbackCommand ToCommand(Guid alertId, ReportFeedbackResource resource) =>
        new(alertId, resource.ReportedBy, Classification(resource.Classification), resource.Reason);

    public static ReviewFeedbackCommand ToCommand(Guid alertId, ReviewFeedbackResource resource) =>
        new(alertId, resource.ReviewedBy, Classification(resource.Classification), resource.Observation);

    public static EvaluationResource ToResource(AlertEvaluation entity) =>
        new(entity.Id, entity.OperatorId, entity.OperationId, entity.IssuedAt, entity.Status,
            entity.EffectiveClassification is { } classification ? Classification(classification) : null,
            entity.Feedback.Select(item => new FeedbackEntryResource(item.ReportedBy, Classification(item.Classification), item.Reason, item.ReportedAt)).ToArray(),
            entity.Reviews.Select(item => new ReviewResource(item.ReviewedBy, Classification(item.Classification), item.Observation, item.ReviewedAt)).ToArray(), entity.Version);
}
