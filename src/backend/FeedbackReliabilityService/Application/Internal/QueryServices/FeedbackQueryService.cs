using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using FeedbackReliabilityService.Domain.Repositories;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Domain.Model;

namespace FeedbackReliabilityService.Application.Internal.QueryServices;

public class ReliabilityOptions
{
    // Operational display threshold, configurable and not a statistical confidence guarantee.
    public int MinimumSampleSize { get; set; } = 30;
}

public record EvaluationPage(IReadOnlyList<AlertEvaluation> Items, int Total, int Page, int PageSize);

public class FeedbackQueryService(IAlertEvaluationRepository repository, ICurrentActor actor, IOptions<ReliabilityOptions> options)
{
    public async Task<AlertEvaluation> GetAsync(Guid alertId, CancellationToken cancellationToken)
    {
        var evaluation = await repository.FindAsync(alertId, cancellationToken) ?? throw new FeedbackNotFoundException();
        actor.RequireOperation(evaluation.OperationId);
        if (!actor.IsInRole("Supervisor") && !actor.IsInRole("Manager") && !actor.IsInRole("Service") &&
            !(actor.IsInRole("Operator") && actor.UserId == evaluation.OperatorId)) throw new FeedbackForbiddenException();
        return evaluation;
    }

    public async Task<EvaluationPage> ListAsync(EvaluationQuery query, string? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        RequireReader(query);
        if (page < 1 || page > 1000000 || pageSize is < 1 or > 100) throw new DomainException("Invalid pagination.");
        if (status is not null && status is not ("PENDING" or "AWAITING_REVIEW" or "REVIEWED"))
            throw new DomainException("Invalid review status.");
        var alerts = await repository.ListAsync(query, cancellationToken);
        var filtered = alerts.Where(alert => status is null || alert.Status == status).ToArray();
        return new(filtered.Skip((page - 1) * pageSize).Take(pageSize).ToArray(), filtered.Length, page, pageSize);
    }

    public async Task<ReliabilityMetric> ReliabilityAsync(EvaluationQuery query, CancellationToken cancellationToken)
    {
        RequireReader(query);
        var alerts = await repository.ListAsync(query, cancellationToken);
        return ReliabilityMetric.Calculate(query, alerts, options.Value.MinimumSampleSize);
    }

    private void RequireReader(EvaluationQuery query)
    {
        query.Validate();
        actor.RequireOperation(query.OperationId);
        if (!actor.IsInRole("Supervisor") && !actor.IsInRole("Manager")) throw new FeedbackForbiddenException();
    }
}
