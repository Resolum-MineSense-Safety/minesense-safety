using FeedbackReliabilityService.Domain.Model.ValueObjects;

namespace FeedbackReliabilityService.Domain.Model.Commands;

public record TrackAlertCommand(Guid AlertId, Guid OperationId);
public record ReportFeedbackCommand(Guid AlertId, Guid ReportedBy, FeedbackClassification Classification, string? Reason);
public record ReviewFeedbackCommand(Guid AlertId, Guid ReviewedBy, FeedbackClassification Classification, string Observation);
