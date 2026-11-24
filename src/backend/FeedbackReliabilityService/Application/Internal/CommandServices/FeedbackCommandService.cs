using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Commands;
using FeedbackReliabilityService.Domain.Repositories;
using FeedbackReliabilityService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Application.Internal.CommandServices;

public class FeedbackCommandService(IAlertEvaluationRepository repository, IAlertSource source,
    ICurrentActor actor, TimeProvider timeProvider) : IFeedbackCommandService
{
    public async Task<AlertEvaluation> Handle(TrackAlertCommand command, CancellationToken cancellationToken)
    {
        if (!actor.IsInRole("Service")) throw new FeedbackForbiddenException();
        actor.RequireOperation(command.OperationId);
        var alert = await source.FindAsync(command.AlertId, cancellationToken) ?? throw new FeedbackNotFoundException();
        var evaluation = new AlertEvaluation(alert.Id, alert.OperatorId, command.OperationId, alert.IssuedAt);
        if (await repository.TryAddAsync(evaluation, cancellationToken)) return evaluation;
        var existing = await repository.FindAsync(command.AlertId, cancellationToken);
        if (existing is not null && existing.OperatorId == evaluation.OperatorId &&
            existing.OperationId == evaluation.OperationId && existing.IssuedAt == evaluation.IssuedAt)
            return existing;
        throw new DomainConflictException("The alert is already tracked with different context.");
    }

    public async Task<AlertEvaluation> Handle(ReportFeedbackCommand command, CancellationToken cancellationToken)
    {
        var evaluation = await GetAuthorizedAsync(command.AlertId, cancellationToken);
        if (command.ReportedBy != actor.UserId ||
            (!actor.IsInRole("Supervisor") && !(actor.IsInRole("Operator") && evaluation.OperatorId == actor.UserId)))
            throw new FeedbackForbiddenException();
        var updated = evaluation.Report(command.ReportedBy, command.Classification, command.Reason, timeProvider.GetUtcNow());
        await SaveAsync(updated, evaluation.Version, cancellationToken);
        return updated;
    }

    public async Task<AlertEvaluation> Handle(ReviewFeedbackCommand command, CancellationToken cancellationToken)
    {
        var evaluation = await GetAuthorizedAsync(command.AlertId, cancellationToken);
        if (!actor.IsInRole("Supervisor") || actor.UserId != command.ReviewedBy) throw new FeedbackForbiddenException();
        var updated = evaluation.Review(command.ReviewedBy, command.Classification, command.Observation, timeProvider.GetUtcNow());
        await SaveAsync(updated, evaluation.Version, cancellationToken);
        return updated;
    }

    private async Task<AlertEvaluation> GetAuthorizedAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var evaluation = await repository.FindAsync(alertId, cancellationToken) ?? throw new FeedbackNotFoundException();
        actor.RequireOperation(evaluation.OperationId);
        return evaluation;
    }

    private async Task SaveAsync(AlertEvaluation updated, int expectedVersion, CancellationToken cancellationToken)
    {
        if (!await repository.TryReplaceAsync(updated, expectedVersion, cancellationToken))
            throw new DomainConflictException("The evaluation changed concurrently; reload before retrying.");
    }
}
