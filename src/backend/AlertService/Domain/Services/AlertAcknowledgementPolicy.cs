using AlertService.Domain.Model.ValueObjects;

namespace AlertService.Domain.Services;

/// <summary>
/// Time the operator has to acknowledge an alert before it is escalated to the supervisor (EP03).
/// </summary>
public static class AlertAcknowledgementPolicy
{
    private static readonly TimeSpan CriticalDeadline = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan WarningDeadline = TimeSpan.FromMinutes(2);

    public static TimeSpan DeadlineFor(AlertSeverity severity) =>
        severity == AlertSeverity.Critical ? CriticalDeadline : WarningDeadline;
}
