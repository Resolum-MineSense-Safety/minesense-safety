namespace FeedbackReliabilityService.Domain.Model.ValueObjects;

public record FeedbackEntry(Guid ReportedBy, FeedbackClassification Classification,
    string? Reason, DateTimeOffset ReportedAt);

public record SupervisorReview(Guid ReviewedBy, FeedbackClassification Classification,
    string Observation, DateTimeOffset ReviewedAt);
