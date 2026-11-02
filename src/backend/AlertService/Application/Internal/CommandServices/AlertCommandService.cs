using AlertService.Application.Internal.IntegrationEvents;
using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Repositories;
using AlertService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AlertService.Application.Internal.CommandServices;

public class AlertCommandService(
    IAlertRepository alertRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher eventPublisher) : IAlertCommandService
{
    private const string Source = "alert-service";

    /// <summary>
    /// Issues the alert of an assessment. One assessment produces at most one alert, so the
    /// FatigueAssessed event and a direct call for the same assessment never duplicate it.
    /// </summary>
    public async Task<Alert> Handle(IssueAlertCommand command)
    {
        if (command.AssessmentId != Guid.Empty &&
            await alertRepository.FindByAssessmentIdAsync(command.AssessmentId) is { } existing)
            return existing;

        var alert = new Alert(command, timeProvider.GetUtcNow());
        await alertRepository.AddAsync(alert);
        await PublishAsync(IntegrationEventTopics.AlertIssued, AlertIssuedEvent.Type, alert,
            new AlertIssuedEvent(alert.Id, alert.OperatorId, alert.AssessmentId, alert.Severity.ToString(),
                alert.IssuedAt), alert.IssuedAt);
        return alert;
    }

    public async Task<Alert?> Handle(AcknowledgeAlertCommand command)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId);
        if (alert is null) return null;

        var acknowledgedAt = timeProvider.GetUtcNow();
        alert.Acknowledge(acknowledgedAt);
        await alertRepository.UpdateAsync(alert);
        await PublishAsync(IntegrationEventTopics.AlertAcknowledged, AlertAcknowledgedEvent.Type, alert,
            new AlertAcknowledgedEvent(alert.Id, alert.OperatorId, alert.Severity.ToString(), acknowledgedAt),
            acknowledgedAt);
        return alert;
    }

    public async Task<Alert?> Handle(EscalateAlertCommand command)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId);
        if (alert is null) return null;

        alert.Escalate();
        await alertRepository.UpdateAsync(alert);
        await PublishEscalatedAsync(alert, timeProvider.GetUtcNow());
        return alert;
    }

    public async Task<IEnumerable<Alert>> Handle(EscalateOverdueAlertsCommand command)
    {
        var now = timeProvider.GetUtcNow();
        var issued = await alertRepository.FindByStatusAsync(AlertStatus.Issued);
        var overdue = issued.Where(alert => alert.IsAcknowledgementOverdue(now)).ToList();

        foreach (var alert in overdue)
        {
            alert.Escalate();
            await alertRepository.UpdateAsync(alert);
            await PublishEscalatedAsync(alert, now);
        }

        return overdue;
    }

    private Task PublishEscalatedAsync(Alert alert, DateTimeOffset escalatedAt) =>
        PublishAsync(IntegrationEventTopics.AlertEscalated, AlertEscalatedEvent.Type, alert,
            new AlertEscalatedEvent(alert.Id, alert.OperatorId, alert.Severity.ToString(), escalatedAt), escalatedAt);

    /// <summary>Events of one alert share the correlation id of the assessment that originated it.</summary>
    private Task PublishAsync<TPayload>(string topic, string type, Alert alert, TPayload payload,
        DateTimeOffset occurredAt) =>
        eventPublisher.PublishAsync(topic, IntegrationEvent.Create(type, Source, alert.Id, payload, occurredAt,
            correlationId: IntegrationEventContext.CorrelationId ?? NullIfEmpty(alert.AssessmentId)));

    private static Guid? NullIfEmpty(Guid id) => id == Guid.Empty ? null : id;
}
