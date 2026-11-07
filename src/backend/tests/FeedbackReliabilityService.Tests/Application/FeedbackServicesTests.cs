using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Application.Internal.CommandServices;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Application.Internal.QueryServices;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Commands;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using FeedbackReliabilityService.Domain.Repositories;
using FeedbackReliabilityService.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Tests.Application;

public class FeedbackServicesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Report_ConcurrentChange_RaisesConflictInsteadOfLosingUpdate()
    {
        // Arrange
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        var actor = new Actor(alert.OperatorId, "Operator", alert.OperationId);
        var repository = new ConflictingRepository(alert);
        var service = new FeedbackCommandService(repository, new AlertSource(null), actor, new Clock());

        // Act
        var action = () => service.Handle(new ReportFeedbackCommand(alert.Id, actor.UserId, FeedbackClassification.Confirmed, null), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DomainConflictException>(action);
        Assert.Empty(alert.Feedback);
    }

    [Fact]
    public async Task Track_ExistingAlertCannotMoveToDifferentOperation()
    {
        // Arrange
        var repository = new InMemoryAlertEvaluationRepository();
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.TryAddAsync(alert);
        var differentOperation = Guid.NewGuid();
        var actor = new Actor(Guid.NewGuid(), "Service", differentOperation);
        var service = new FeedbackCommandService(repository, new AlertSource(new(alert.Id, alert.OperatorId, Now)), actor, new Clock());

        // Act
        var action = () => service.Handle(new TrackAlertCommand(alert.Id, differentOperation), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DomainConflictException>(action);
        Assert.Equal(alert.OperationId, (await repository.FindAsync(alert.Id))!.OperationId);
    }

    [Fact]
    public async Task Track_AlertDoesNotExist_RejectsIt()
    {
        // Arrange
        var actor = new Actor(Guid.NewGuid(), "Service", Guid.NewGuid());
        var service = new FeedbackCommandService(new InMemoryAlertEvaluationRepository(), new AlertSource(null), actor, new Clock());

        // Act
        var action = () => service.Handle(new TrackAlertCommand(Guid.NewGuid(), actor.OperationId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<FeedbackNotFoundException>(action);
    }

    [Theory]
    [InlineData("Operator")]
    [InlineData("Supervisor")]
    [InlineData("Manager")]
    public async Task Track_WithoutServiceRole_RejectsIt(string role)
    {
        // Arrange
        var actor = new Actor(Guid.NewGuid(), role, Guid.NewGuid());
        var service = new FeedbackCommandService(new InMemoryAlertEvaluationRepository(), new AlertSource(null), actor, new Clock());

        // Act
        var action = () => service.Handle(new TrackAlertCommand(Guid.NewGuid(), actor.OperationId), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<FeedbackForbiddenException>(action);
    }

    [Fact]
    public async Task Query_OperatorCannotReadAnotherOperatorsHistory()
    {
        // Arrange
        var repository = new InMemoryAlertEvaluationRepository();
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.TryAddAsync(alert);
        var query = new FeedbackQueryService(repository, new Actor(Guid.NewGuid(), "Operator", alert.OperationId), Options.Create(new ReliabilityOptions()));

        // Act
        var action = () => query.GetAsync(alert.Id, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<FeedbackForbiddenException>(action);
    }

    [Fact]
    public async Task Query_ManagerUsesConfiguredSampleThreshold()
    {
        // Arrange
        var repository = new InMemoryAlertEvaluationRepository();
        var alert = new AlertEvaluation(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.TryAddAsync(alert.Report(alert.OperatorId, FeedbackClassification.Confirmed, null, Now));
        var query = new FeedbackQueryService(repository, new Actor(Guid.NewGuid(), "Manager", alert.OperationId),
            Options.Create(new ReliabilityOptions { MinimumSampleSize = 1 }));

        // Act
        var metric = await query.ReliabilityAsync(new(alert.OperationId, Now, Now.AddDays(1)), CancellationToken.None);

        // Assert
        Assert.Equal(1, metric.MinimumSampleSize);
        Assert.False(metric.InsufficientSample);
    }

    private sealed class Actor(Guid userId, string role, Guid operationId) : ICurrentActor
    {
        public Guid UserId => userId;
        public Guid OperationId => operationId;
        public bool IsInRole(string candidate) => role == candidate;
        public void RequireOperation(Guid id)
        {
            if (id != operationId) throw new FeedbackForbiddenException();
        }
    }

    private sealed class AlertSource(SourceAlert? alert) : IAlertSource
    {
        public Task<SourceAlert?> FindAsync(Guid alertId, CancellationToken cancellationToken) => Task.FromResult(alert);
    }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ConflictingRepository(AlertEvaluation evaluation) : IAlertEvaluationRepository
    {
        public Task<AlertEvaluation?> FindAsync(Guid alertId, CancellationToken cancellationToken = default) => Task.FromResult<AlertEvaluation?>(evaluation);
        public Task<bool> TryReplaceAsync(AlertEvaluation updated, int expectedVersion, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> TryAddAsync(AlertEvaluation updated, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AlertEvaluation>> ListAsync(EvaluationQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
