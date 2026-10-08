using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AlertService.Domain.Model.Aggregates;

/// <summary>
/// Preventive warning delivered in the cabin (EP03). An issued alert must be
/// acknowledged by the operator; otherwise it is escalated to the supervisor.
/// </summary>
public class Alert : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public Guid AssessmentId { get; private set; }
    public AlertSeverity Severity { get; private set; }
    public AlertStatus Status { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset? AcknowledgedAt { get; private set; }

    public Alert(IssueAlertCommand command, DateTimeOffset issuedAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("An alert must belong to an operator.");

        OperatorId = command.OperatorId;
        AssessmentId = command.AssessmentId;
        Severity = command.Severity;
        Status = AlertStatus.Issued;
        IssuedAt = issuedAt;
    }

    public void Acknowledge(DateTimeOffset acknowledgedAt)
    {
        if (Status != AlertStatus.Issued)
            throw new DomainException($"Only issued alerts can be acknowledged; current status is {Status}.");

        Status = AlertStatus.Acknowledged;
        AcknowledgedAt = acknowledgedAt;
    }

    public void Escalate()
    {
        if (Status != AlertStatus.Issued)
            throw new DomainException($"Only unacknowledged alerts can be escalated; current status is {Status}.");

        Status = AlertStatus.Escalated;
    }

    // A1 (Domain): true while the alert is Issued and its deadline (AlertAcknowledgementPolicy:
    // 30 s for Critical, 2 min for Warning) has passed.
    public bool IsAcknowledgementOverdue(DateTimeOffset now) =>
        throw new NotImplementedException("A1: pending implementation.");
}
