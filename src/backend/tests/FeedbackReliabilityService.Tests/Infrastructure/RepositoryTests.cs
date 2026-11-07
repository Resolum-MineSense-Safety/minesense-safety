using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using FeedbackReliabilityService.Infrastructure.Persistence.InMemory;
using FeedbackReliabilityService.Infrastructure.Persistence.MongoDb;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace FeedbackReliabilityService.Tests.Infrastructure;

public class RepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task InMemory_ConcurrentWriters_OnlyOneVersionCanWin()
    {
        // Arrange
        var repository = new InMemoryAlertEvaluationRepository();
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.TryAddAsync(alert);
        var first = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now);
        var second = alert.Report(Guid.NewGuid(), FeedbackClassification.Inconclusive, null, Now);

        // Act
        var results = await Task.WhenAll(repository.TryReplaceAsync(first, 0), repository.TryReplaceAsync(second, 0));

        // Assert
        Assert.Single(results, result => result);
        Assert.Equal(1, (await repository.FindAsync(alert.Id))!.Version);
        Assert.False(await repository.TryAddAsync(alert));
    }

    [Fact]
    public async Task InMemory_FiltersScopePeriodAndOrdersNewestFirst()
    {
        // Arrange
        var repository = new InMemoryAlertEvaluationRepository();
        var scope = Guid.NewGuid();
        var first = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), scope, Now);
        var second = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), scope, Now.AddHours(1));
        await repository.TryAddAsync(second);
        await repository.TryAddAsync(first);
        await repository.TryAddAsync(new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now));
        await repository.TryAddAsync(new(Guid.NewGuid(), Guid.NewGuid(), scope, Now.AddDays(1)));

        // Act
        var result = await repository.ListAsync(new EvaluationQuery(scope, Now, Now.AddDays(1)));

        // Assert
        Assert.Equal(new[] { second.Id, first.Id }, result.Select(item => item.Id));
    }

    [Fact]
    public void MongoDocument_BsonRoundTrip_PreservesIdentityHistoryAndUtcTime()
    {
        // Arrange
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now.ToOffset(TimeSpan.FromHours(-5)));
        var reviewed = alert.Report(alert.OperatorId, FeedbackClassification.FalseAlarm, "Dust", Now)
            .Review(Guid.NewGuid(), FeedbackClassification.Inconclusive, "Check camera", Now.AddMinutes(1));

        // Act
        var document = EvaluationDocument.FromEntity(reviewed);
        var restored = BsonSerializer.Deserialize<EvaluationDocument>(document.ToBson()).ToEntity();

        // Assert
        Assert.Equal(reviewed.Id, restored.Id);
        Assert.Equal(reviewed.IssuedAt, restored.IssuedAt);
        Assert.Equal(reviewed.Feedback, restored.Feedback);
        Assert.Equal(reviewed.Reviews, restored.Reviews);
        Assert.Equal(reviewed.EffectiveClassification, restored.EffectiveClassification);
        Assert.Equal(reviewed.Version, restored.Version);
    }

    [MongoFact]
    public async Task Mongo_RealServer_RestartPersistenceAndConcurrency()
    {
        // Arrange
        var connection = Environment.GetEnvironmentVariable("FEEDBACK_TEST_MONGODB");
        var client = new MongoClient(connection);
        var name = "feedback_tests_" + Guid.NewGuid().ToString("N");
        var database = client.GetDatabase(name);
        var repository = new MongoAlertEvaluationRepository(database);
        await repository.InitializeAsync(CancellationToken.None);
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        try
        {
            // Act
            Assert.True(await repository.TryAddAsync(alert));
            Assert.False(await repository.TryAddAsync(alert));
            var updated = alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now);
            var writes = await Task.WhenAll(repository.TryReplaceAsync(updated, 0), repository.TryReplaceAsync(updated, 0));
            var restarted = new MongoAlertEvaluationRepository(database);
            var restored = await restarted.FindAsync(alert.Id);
            var listed = await restarted.ListAsync(new(alert.OperationId, Now, Now.AddDays(1)));

            // Assert
            Assert.Single(writes, result => result);
            Assert.Equal(FeedbackClassification.Confirmed, restored!.EffectiveClassification);
            Assert.Equal(alert.Id, Assert.Single(listed).Id);
        }
        finally
        {
            await client.DropDatabaseAsync(name);
        }
    }
}

public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("FEEDBACK_TEST_MONGODB")))
            Skip = "Set FEEDBACK_TEST_MONGODB to run against a real MongoDB server; dedicated CI always sets it.";
    }
}
