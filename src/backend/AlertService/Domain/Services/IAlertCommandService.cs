using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;

namespace AlertService.Domain.Services;

public interface IAlertCommandService
{
    Task<Alert> Handle(IssueAlertCommand command);

    /// <summary>Records a warning the cabin already gave; recording it again returns the same alert.</summary>
    Task<Alert> Handle(RecordCabinWarningCommand command);
    Task<Alert?> Handle(AcknowledgeAlertCommand command);
    Task<Alert?> Handle(EscalateAlertCommand command);

    /// <summary>Returns the alerts that were escalated.</summary>
    Task<IEnumerable<Alert>> Handle(EscalateOverdueAlertsCommand command);
}
