using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Repositories;
using AlertService.Domain.Services;

namespace AlertService.Application.Internal.CommandServices;

public class AlertCommandService(IAlertRepository alertRepository, TimeProvider timeProvider) : IAlertCommandService
{
    public async Task<Alert> Handle(IssueAlertCommand command)
    {
        var alert = new Alert(command, timeProvider.GetUtcNow());
        await alertRepository.AddAsync(alert);
        return alert;
    }

    public async Task<Alert?> Handle(AcknowledgeAlertCommand command)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId);
        if (alert is null) return null;

        alert.Acknowledge(timeProvider.GetUtcNow());
        await alertRepository.UpdateAsync(alert);
        return alert;
    }

    public async Task<Alert?> Handle(EscalateAlertCommand command)
    {
        var alert = await alertRepository.FindByIdAsync(command.AlertId);
        if (alert is null) return null;

        alert.Escalate();
        await alertRepository.UpdateAsync(alert);
        return alert;
    }

    // A2 (Application): escalate the issued alerts that are overdue and return them.
    public Task<IEnumerable<Alert>> Handle(EscalateOverdueAlertsCommand command) =>
        throw new NotImplementedException("A2: pending implementation.");
}
