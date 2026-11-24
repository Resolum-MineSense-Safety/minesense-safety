using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FeedbackReliabilityService.Interfaces.REST.Resources;

public record TrackAlertResource([property: JsonRequired] Guid OperationId);
public record ReportFeedbackResource(
    [Required, RegularExpression("^(CONFIRMED|FALSE_ALARM|INCONCLUSIVE)$")] string Classification,
    [MaxLength(1000)] string? Reason,
    [property: JsonRequired] Guid ReportedBy);
public record ReviewFeedbackResource(
    [Required, RegularExpression("^(CONFIRMED|FALSE_ALARM|INCONCLUSIVE)$")] string Classification,
    [Required, StringLength(1000, MinimumLength = 1)] string Observation,
    [property: JsonRequired] Guid ReviewedBy);
public record FeedbackEntryResource(Guid ReportedBy, string Classification, string? Reason, DateTimeOffset ReportedAt);
public record ReviewResource(Guid ReviewedBy, string Classification, string Observation, DateTimeOffset ReviewedAt);
public record EvaluationResource(Guid AlertId, Guid OperatorId, Guid OperationId, DateTimeOffset IssuedAt,
    string Status, string? EffectiveClassification, IReadOnlyList<FeedbackEntryResource> Feedback,
    IReadOnlyList<ReviewResource> Reviews, int Version);
public record EvaluationPageResource(IReadOnlyList<EvaluationResource> Items, int Total, int Page, int PageSize);
