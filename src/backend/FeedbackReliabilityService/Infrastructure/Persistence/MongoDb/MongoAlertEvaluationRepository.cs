using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Repositories;
using MongoDB.Driver;

namespace FeedbackReliabilityService.Infrastructure.Persistence.MongoDb;

public class MongoAlertEvaluationRepository(IMongoDatabase database) : IAlertEvaluationRepository
{
    private readonly IMongoCollection<EvaluationDocument> _collection = database.GetCollection<EvaluationDocument>("alert_evaluations");

    public Task InitializeAsync(CancellationToken cancellationToken) => _collection.Indexes.CreateOneAsync(
        new CreateIndexModel<EvaluationDocument>(Builders<EvaluationDocument>.IndexKeys
            .Ascending(item => item.OperationId).Descending(item => item.IssuedAt).Ascending(item => item.Id)),
        cancellationToken: cancellationToken);

    public async Task<AlertEvaluation?> FindAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var document = await _collection.Find(item => item.Id == alertId.ToString()).FirstOrDefaultAsync(cancellationToken);
        return document?.ToEntity();
    }

    public async Task<bool> TryAddAsync(AlertEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        try
        {
            await _collection.InsertOneAsync(EvaluationDocument.FromEntity(evaluation), cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task<bool> TryReplaceAsync(AlertEvaluation evaluation, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var result = await _collection.ReplaceOneAsync(item => item.Id == evaluation.Id.ToString() && item.Version == expectedVersion,
            EvaluationDocument.FromEntity(evaluation), cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<IReadOnlyList<AlertEvaluation>> ListAsync(EvaluationQuery query, CancellationToken cancellationToken = default)
    {
        query.Validate();
        var documents = await _collection.Find(item => item.OperationId == query.OperationId.ToString() &&
                item.IssuedAt >= query.From.UtcDateTime && item.IssuedAt < query.To.UtcDateTime)
            .SortByDescending(item => item.IssuedAt).ThenBy(item => item.Id).ToListAsync(cancellationToken);
        return documents.Select(item => item.ToEntity()).ToArray();
    }
}
