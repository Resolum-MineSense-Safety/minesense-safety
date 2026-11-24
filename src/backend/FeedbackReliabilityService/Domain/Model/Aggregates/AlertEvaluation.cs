using FeedbackReliabilityService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Domain.Model.Aggregates;

/// <summary>Local alert context and append-only feedback/review history (EP12).</summary>
public class AlertEvaluation : AggregateRoot
{
    public Guid OperatorId { get; }
    public Guid OperationId { get; }
    public DateTimeOffset IssuedAt { get; }
    public IReadOnlyList<FeedbackEntry> Feedback { get; }
    public IReadOnlyList<SupervisorReview> Reviews { get; }
    public int Version { get; }
    public FeedbackClassification? EffectiveClassification =>
        Reviews.LastOrDefault()?.Classification ?? Feedback.FirstOrDefault()?.Classification;
    public string Status => Feedback.Count == 0 ? "PENDING" : Reviews.Count == 0 ? "AWAITING_REVIEW" : "REVIEWED";

    public AlertEvaluation(Guid alertId, Guid operatorId, Guid operationId, DateTimeOffset issuedAt)
        : this(alertId, operatorId, operationId, issuedAt, [], [], 0) { }

    // Rehydration preserves the original server timestamps and review history.
    public AlertEvaluation(Guid alertId, Guid operatorId, Guid operationId, DateTimeOffset issuedAt,
        IEnumerable<FeedbackEntry> feedback, IEnumerable<SupervisorReview> reviews, int version)
    {
        if (alertId == Guid.Empty || operatorId == Guid.Empty || operationId == Guid.Empty)
            throw new DomainException("Alert, operator and operation identifiers are required.");
        Id = alertId;
        OperatorId = operatorId;
        OperationId = operationId;
        IssuedAt = issuedAt;
        Feedback = Array.AsReadOnly(feedback.ToArray());
        Reviews = Array.AsReadOnly(reviews.ToArray());
        Version = version;
    }

    public AlertEvaluation Report(Guid reportedBy, FeedbackClassification classification, string? reason, DateTimeOffset now)
    {
        ValidateClassification(classification);
        if (reportedBy == Guid.Empty) throw new DomainException("A reporter is required.");
        if (Feedback.Any(entry => entry.ReportedBy == reportedBy))
            throw new DomainConflictException("This user already evaluated the alert.");
        if (Reviews.Count > 0) throw new DomainConflictException("The alert has already been reviewed.");
        if (classification == FeedbackClassification.FalseAlarm && string.IsNullOrWhiteSpace(reason))
            throw new DomainException("A false alarm requires a reason.");
        if (reason?.Length > 1000) throw new DomainException("Reason must not exceed 1000 characters.");
        if (now < IssuedAt) throw new DomainException("Feedback cannot precede the alert.");
        return new(Id, OperatorId, OperationId, IssuedAt,
            Feedback.Append(new(reportedBy, classification, reason?.Trim(), now)), Reviews, Version + 1);
    }

    public AlertEvaluation Review(Guid reviewedBy, FeedbackClassification classification, string observation, DateTimeOffset now)
    {
        ValidateClassification(classification);
        if (reviewedBy == Guid.Empty) throw new DomainException("A reviewer is required.");
        if (Feedback.Count == 0) throw new DomainConflictException("An unclassified alert remains pending evaluation.");
        if (string.IsNullOrWhiteSpace(observation) || observation.Length > 1000)
            throw new DomainException("An observation of 1 to 1000 characters is required.");
        var previousTime = Reviews.LastOrDefault()?.ReviewedAt ?? Feedback.Max(entry => entry.ReportedAt);
        if (now < previousTime) throw new DomainException("A review cannot precede the previous evaluation.");
        return new(Id, OperatorId, OperationId, IssuedAt, Feedback,
            Reviews.Append(new(reviewedBy, classification, observation.Trim(), now)), Version + 1);
    }

    private static void ValidateClassification(FeedbackClassification classification)
    {
        if (!Enum.IsDefined(classification)) throw new DomainException("Invalid feedback classification.");
    }
}
