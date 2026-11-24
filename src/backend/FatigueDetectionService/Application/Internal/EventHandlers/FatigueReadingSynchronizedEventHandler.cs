using FatigueDetectionService.Application.Internal.IntegrationEvents;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Domain.Model;

namespace FatigueDetectionService.Application.Internal.EventHandlers;

/// <summary>
/// EP06 -> EP02 (US14): a reading classified in the cabin becomes a historical assessment with its
/// capture time. Idempotent per reading. A reading the domain rejects cannot succeed on a retry, so
/// it is logged and dropped; it stays stored in its batch.
/// </summary>
public class FatigueReadingSynchronizedEventHandler(
    IFatigueAssessmentCommandService commandService,
    ILogger<FatigueReadingSynchronizedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.EdgeFatigueReadings;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var reading = integrationEvent.GetPayload<FatigueReadingSynchronizedEvent>();
        var data = reading.Data;
        try
        {
            var assessment = await commandService.Handle(new RecordEdgeAssessmentCommand(reading.EdgeEventId,
                data.OperatorId, data.MonitoringSessionId, data.Perclos, data.BlinkRatePerMinute,
                data.HeartRateVariabilityMs, reading.CapturedAt));

            // The thresholds must match in both places; a difference means they were not recalibrated together.
            if (!string.Equals(data.RiskLevel, assessment.RiskLevel.ToString(), StringComparison.OrdinalIgnoreCase))
                logger.LogWarning("Reading {EdgeEventId} was {CabinRisk} in the cabin and {CloudRisk} in the cloud",
                    reading.EdgeEventId, data.RiskLevel, assessment.RiskLevel);
        }
        catch (DomainException exception) when (exception is not DomainConflictException)
        {
            logger.LogError("Reading {EdgeEventId} from the Edge was rejected: {Reason}",
                reading.EdgeEventId, exception.Message);
        }
    }
}
