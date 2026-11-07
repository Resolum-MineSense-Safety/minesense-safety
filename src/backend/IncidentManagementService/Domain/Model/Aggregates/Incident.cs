using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.Entities;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Domain.Model.Aggregates;

/// <summary>
/// Fatigue incident handled from the control center (EP05): it is received,
/// taken by one supervisor, answered with documented actions, escalated when a
/// critical case stays unattended and finally closed with its resolution.
/// </summary>
public class Incident : AggregateRoot
{
    private readonly List<IncidentAction> _actions = [];
    private readonly List<IncidentEvent> _history = [];

    public Guid OperatorId { get; private set; }
    public Guid AlertId { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public IncidentStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public Guid? AssigneeId { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public int EscalationLevel { get; private set; }
    public DateTimeOffset? LastEscalatedAt { get; private set; }
    public string? Resolution { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public IReadOnlyList<IncidentAction> Actions => _actions;
    public IReadOnlyList<IncidentEvent> History => _history;

    public Incident(OpenIncidentCommand command, DateTimeOffset openedAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("An incident must belong to an operator.");
        if (command.AlertId == Guid.Empty)
            throw new DomainException("An incident must reference the alert that originated it.");

        OperatorId = command.OperatorId;
        AlertId = command.AlertId;
        Severity = command.Severity;
        Status = IncidentStatus.Pending;
        OpenedAt = openedAt;
        _history.Add(new IncidentEvent(IncidentEventType.Opened, null, openedAt,
            $"{Severity} incident received from alert {AlertId}."));
    }

    /// <summary>Time between the incident reception and its closure.</summary>
    public TimeSpan? AttentionTime => ClosedAt - OpenedAt;

    public void Assign(Guid assigneeId, DateTimeOffset assignedAt)
    {
        if (assigneeId == Guid.Empty)
            throw new DomainException("An incident must be assigned to a supervisor.");
        if (Status == IncidentStatus.Closed)
            throw new DomainConflictException("A closed incident cannot be assigned.");
        if (Status == IncidentStatus.Assigned)
        {
            if (AssigneeId == assigneeId) return;
            throw new DomainConflictException($"Incident already assigned to supervisor {AssigneeId}.");
        }

        Status = IncidentStatus.Assigned;
        AssigneeId = assigneeId;
        AssignedAt = assignedAt;
        _history.Add(new IncidentEvent(IncidentEventType.Assigned, assigneeId, assignedAt,
            "Supervisor took ownership of the incident."));
    }

    public IncidentAction RecordAction(IncidentActionType actionType, string description, string result,
        Guid performedBy, DateTimeOffset performedAt)
    {
        if (Status == IncidentStatus.Closed)
            throw new DomainConflictException("Actions cannot be recorded on a closed incident.");
        if (performedAt < OpenedAt)
            throw new DomainException("An action cannot happen before the incident was opened.");

        var action = new IncidentAction(actionType, description, result, performedBy, performedAt);
        _actions.Add(action);
        _history.Add(new IncidentEvent(IncidentEventType.ActionRecorded, performedBy, performedAt,
            $"{actionType}: {action.Description} Result: {action.Result}"));
        return action;
    }

    public void Close(string resolution, DateTimeOffset closedAt)
    {
        if (Status == IncidentStatus.Closed)
            throw new DomainConflictException("The incident is already closed.");
        if (_actions.Count == 0)
            throw new DomainConflictException("The incident still requires attention: no action has been recorded.");
        if (string.IsNullOrWhiteSpace(resolution))
            throw new DomainException("Closing an incident requires a documented resolution.");
        if (closedAt < OpenedAt)
            throw new DomainException("An incident cannot be closed before it was opened.");

        Status = IncidentStatus.Closed;
        Resolution = resolution.Trim();
        ClosedAt = closedAt;
        _history.Add(new IncidentEvent(IncidentEventType.Closed, AssigneeId, closedAt, Resolution));
    }

    /// <summary>Only critical incidents take part in the escalation chain (TS-19).</summary>
    public bool IsEscalationApplicable => Severity == IncidentSeverity.Critical;

    /// <summary>
    /// True when a critical incident is still unattended and the attention
    /// deadline counted from its last milestone has passed.
    /// </summary>
    public bool IsEscalationDue(DateTimeOffset now) =>
        IsEscalationApplicable
        && Status == IncidentStatus.Pending
        && EscalationLevel < IncidentEscalationPolicy.MaxLevel
        && now - (LastEscalatedAt ?? OpenedAt) > IncidentEscalationPolicy.AttentionDeadline;

    public void Escalate(DateTimeOffset escalatedAt)
    {
        if (!IsEscalationDue(escalatedAt))
            throw new DomainException("The incident does not meet the escalation conditions.");

        EscalationLevel++;
        LastEscalatedAt = escalatedAt;
        var role = IncidentEscalationPolicy.NotifiedRolesAt(EscalationLevel)[^1];
        _history.Add(new IncidentEvent(IncidentEventType.Escalated, null, escalatedAt,
            $"Escalated to level {EscalationLevel}: {role} notified."));
    }
}
