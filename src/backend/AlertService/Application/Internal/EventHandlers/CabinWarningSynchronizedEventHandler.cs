using AlertService.Application.Internal.IntegrationEvents;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Domain.Model;

namespace AlertService.Application.Internal.EventHandlers;

/// <summary>
/// EP06 -> EP03 (US14): records a warning the cabin already gave, with its original time. The alert
/// shares the id of the reading, which is also the id of the assessment, so one reading never has two
/// alerts. Warnings written before <c>readingEventId</c> existed fall back to their own event id.
/// </summary>
public class CabinWarningSynchronizedEventHandler(
    IAlertCommandService alertCommandService,
    ILogger<CabinWarningSynchronizedEventHandler> logger) : IIntegrationEventHandler
{
    public string Topic => IntegrationEventTopics.EdgeCabinWarnings;

    public async Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
    {
        var warning = integrationEvent.GetPayload<CabinWarningSynchronizedEvent>();
        var data = warning.Data;
        if (!Enum.TryParse<AlertSeverity>(data.Severity, ignoreCase: true, out var severity))
        {
            logger.LogWarning("Cabin warning {EdgeEventId} has an unknown severity {Severity}; not recorded",
                warning.EdgeEventId, data.Severity);
            return;
        }

        try
        {
            var alert = await alertCommandService.Handle(new RecordCabinWarningCommand(data.OperatorId,
                data.ReadingEventId ?? warning.EdgeEventId, severity, warning.CapturedAt));
            logger.LogInformation("Alert {AlertId} ({Severity}) recorded for cabin warning {EdgeEventId}",
                alert.Id, alert.Severity, warning.EdgeEventId);
        }
        catch (DomainException exception) when (exception is not DomainConflictException)
        {
            logger.LogError("Cabin warning {EdgeEventId} was rejected: {Reason}", warning.EdgeEventId, exception.Message);
        }
    }
}
