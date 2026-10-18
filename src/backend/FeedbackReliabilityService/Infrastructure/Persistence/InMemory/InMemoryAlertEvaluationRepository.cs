using System.Collections.Concurrent;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Repositories;

namespace FeedbackReliabilityService.Infrastructure.Persistence.InMemory;

/// <summary>Immutable aggregate snapshots and atomic compare-and-swap updates.</summary>
public class InMemoryAlertEvaluationRepository : IAlertEvaluationRepository
{
    private readonly ConcurrentDictionary<Guid, AlertEvaluation> _store = new();

    public Task<AlertEvaluation?> FindAsync(Guid alertId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.GetValueOrDefault(alertId));

    public Task<bool> TryAddAsync(AlertEvaluation evaluation, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.TryAdd(evaluation.Id, evaluation));

    public Task<bool> TryReplaceAsync(AlertEvaluation evaluation, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var current = _store.GetValueOrDefault(evaluation.Id);
        return Task.FromResult(current is not null && current.Version == expectedVersion &&
            _store.TryUpdate(evaluation.Id, evaluation, current));
    }

    public Task<IReadOnlyList<AlertEvaluation>> ListAsync(EvaluationQuery query, CancellationToken cancellationToken = default)
    {
        query.Validate();
        return Task.FromResult<IReadOnlyList<AlertEvaluation>>(_store.Values
            .Where(alert => alert.OperationId == query.OperationId && alert.IssuedAt >= query.From && alert.IssuedAt < query.To)
            .OrderByDescending(alert => alert.IssuedAt).ThenBy(alert => alert.Id).ToArray());
    }
}
