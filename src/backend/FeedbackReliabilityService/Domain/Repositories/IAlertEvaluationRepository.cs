using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;

namespace FeedbackReliabilityService.Domain.Repositories;

public interface IAlertEvaluationRepository
{
    Task<AlertEvaluation?> FindAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task<bool> TryAddAsync(AlertEvaluation evaluation, CancellationToken cancellationToken = default);
    Task<bool> TryReplaceAsync(AlertEvaluation evaluation, int expectedVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AlertEvaluation>> ListAsync(EvaluationQuery query, CancellationToken cancellationToken = default);
}
