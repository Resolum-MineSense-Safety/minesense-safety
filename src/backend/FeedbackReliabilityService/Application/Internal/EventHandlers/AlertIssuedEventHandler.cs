using FeedbackReliabilityService.Application.Internal.IntegrationEvents;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Repositories;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FeedbackReliabilityService.Application.Internal.EventHandlers;

/// <summary>
/// EP03 -> EP12: registers every issued alert for feedback under the operation of its operator,
/// the event-driven twin of <c>PUT /api/v1/feedback-alerts/{alertId}</c>. The event comes from the
/// Alert Service itself, so no extra lookup is needed. Alerts of operators without a known
/// operation are not tracked (an operation is mandatory) and are logged. Idempotent per alert.
/// </summary>
public class AlertIssuedEventHandler(
    IAlertEvaluationRepository repository,
    IOperatorOperationDirectory directory,
    ILogger<AlertIssuedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.AlertIssued;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var alert = integrationEvent.GetPayload<AlertIssuedEvent>();
        var operationId = await directory.FindOperationIdAsync(alert.OperatorId);
        if (operationId is null)
        {
            logger.LogWarning("Operator {OperatorId} has no known operation; alert {AlertId} not tracked",
                alert.OperatorId, alert.AlertId);
            return;
        }

        var evaluation = new AlertEvaluation(alert.AlertId, alert.OperatorId, operationId.Value, alert.IssuedAt);
        if (!await repository.TryAddAsync(evaluation, cancellationToken))
            logger.LogDebug("Alert {AlertId} was already tracked", alert.AlertId);
    }
}
