using AlertService.Application.Internal.IntegrationEvents;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AlertService.Application.Internal.EventHandlers;

/// <summary>
/// EP02 -> EP03: issues the preventive alert of an assessment that requires one. Issuing is
/// idempotent per assessment, so a redelivered event returns the alert already issued.
/// </summary>
public class FatigueAssessedEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<FatigueAssessedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.FatigueAssessed;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var assessed = integrationEvent.GetPayload<FatigueAssessedEvent>();
        if (!assessed.RequiresAlert) return;

        if (!Enum.TryParse<AlertSeverity>(assessed.RiskLevel, ignoreCase: true, out var severity))
        {
            logger.LogWarning("Assessment {AssessmentId} has an unknown risk level {RiskLevel}; no alert issued",
                assessed.AssessmentId, assessed.RiskLevel);
            return;
        }

        var alert = await alertCommandService.Handle(
            new IssueAlertCommand(assessed.OperatorId, assessed.AssessmentId, severity));
        logger.LogInformation("Alert {AlertId} ({Severity}) issued for assessment {AssessmentId}",
            alert.Id, alert.Severity, assessed.AssessmentId);
    }
}
