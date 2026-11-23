using AnalyticsService.Application.Internal.IntegrationEvents;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ReadModels;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AnalyticsService.Application.Internal.EventHandlers;

/// <summary>
/// EP03 -> EP11: records every issued alert as a fatigue observation with the operator's context
/// taken from the local copy of Fleet Monitoring. When Fleet Monitoring has not reported the
/// operator, the observation is kept with <c>unknown</c> dimensions instead of being lost.
/// Recording is idempotent per alert.
/// </summary>
public class AlertIssuedEventHandler(
    IFatigueMetricCommandService fatigueMetricCommandService,
    IOperatorContextRepository operatorContextRepository,
    ILogger<AlertIssuedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.AlertIssued;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var alert = integrationEvent.GetPayload<AlertIssuedEvent>();
        if (!Enum.TryParse<FatigueSeverity>(alert.Severity, ignoreCase: true, out var severity))
        {
            logger.LogWarning("Alert {AlertId} has an unknown severity {Severity}", alert.AlertId, alert.Severity);
            return;
        }

        var context = await operatorContextRepository.FindByOperatorIdAsync(alert.OperatorId);
        if (context is null)
            logger.LogWarning("No fleet context for operator {OperatorId}; alert {AlertId} recorded as unknown",
                alert.OperatorId, alert.AlertId);

        await fatigueMetricCommandService.Handle(new RecordFatigueMetricCommand(alert.OperatorId, alert.AlertId,
            severity, alert.IssuedAt,
            context?.Shift ?? OperatorContext.Unknown, context?.MineSite ?? OperatorContext.Unknown,
            context?.Location ?? OperatorContext.Unknown, context?.Fleet ?? OperatorContext.Unknown));
    }
}
