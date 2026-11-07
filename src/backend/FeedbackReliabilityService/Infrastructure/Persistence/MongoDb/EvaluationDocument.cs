using System.Text.Json;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.ValueObjects;

namespace FeedbackReliabilityService.Infrastructure.Persistence.MongoDb;

// Persistence DTOs keep BSON/JSON serialization out of the domain.
public record EvaluationSnapshot(Guid AlertId, Guid OperatorId, Guid OperationId, DateTimeOffset IssuedAt,
    FeedbackEntry[] Feedback, SupervisorReview[] Reviews, int Version);

public class EvaluationDocument
{
    public string Id { get; set; } = string.Empty;
    public string OperationId { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int Version { get; set; }
    public string Payload { get; set; } = string.Empty;

    public static EvaluationDocument FromEntity(AlertEvaluation entity) => new()
    {
        Id = entity.Id.ToString(), OperationId = entity.OperationId.ToString(),
        IssuedAt = entity.IssuedAt.UtcDateTime, Version = entity.Version,
        Payload = JsonSerializer.Serialize(new EvaluationSnapshot(entity.Id, entity.OperatorId, entity.OperationId,
            entity.IssuedAt, entity.Feedback.ToArray(), entity.Reviews.ToArray(), entity.Version))
    };

    public AlertEvaluation ToEntity()
    {
        var snapshot = JsonSerializer.Deserialize<EvaluationSnapshot>(Payload)
            ?? throw new InvalidOperationException("Invalid stored evaluation.");
        return new(snapshot.AlertId, snapshot.OperatorId, snapshot.OperationId, snapshot.IssuedAt,
            snapshot.Feedback, snapshot.Reviews, snapshot.Version);
    }
}
