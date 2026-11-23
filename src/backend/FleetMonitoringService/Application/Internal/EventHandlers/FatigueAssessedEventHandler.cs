using FleetMonitoringService.Application.Internal.IntegrationEvents;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FleetMonitoringService.Application.Internal.EventHandlers;

/// <summary>
/// EP02 -> EP04: every assessment sets the current risk level of the operator in the control
/// center. Setting the same level again is harmless, so redeliveries need no extra check.
/// </summary>
public class FatigueAssessedEventHandler(
    IMonitoredOperatorCommandService monitoredOperatorCommandService,
    ILogger<FatigueAssessedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.FatigueAssessed;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var assessed = integrationEvent.GetPayload<FatigueAssessedEvent>();
        if (!Enum.TryParse<RiskLevel>(assessed.RiskLevel, ignoreCase: true, out var riskLevel))
        {
            logger.LogWarning("Assessment {AssessmentId} has an unknown risk level {RiskLevel}",
                assessed.AssessmentId, assessed.RiskLevel);
            return;
        }

        var monitoredOperator = await monitoredOperatorCommandService.Handle(
            new UpdateOperatorRiskLevelCommand(assessed.OperatorId, riskLevel));
        if (monitoredOperator is null)
            logger.LogInformation("Operator {OperatorId} is not monitored; risk level {RiskLevel} ignored",
                assessed.OperatorId, riskLevel);
    }
}
