using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Domain.Model.Aggregates;

/// <summary>
/// Fatigue incident opened from an alert (EP05). It is assigned to a supervisor,
/// who registers the response actions before closing it; it may be escalated
/// while unresolved. Every step is kept for auditing.
/// </summary>
public class Incident : AggregateRoot
{
    private readonly List<IncidentAction> _actions = [];

    public Guid AlertId { get; private set; }
    public Guid OperatorId { get; private set; }
    public IncidentStatus Status { get; private set; }
    public DateTimeOffset OpenedAt { get; private set; }
    public Guid? AssignedSupervisorId { get; private set; }
    public DateTimeOffset? AssignedAt { get; private set; }
    public IReadOnlyList<IncidentAction> Actions => _actions.AsReadOnly();
    public string? Resolution { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    public Incident(OpenIncidentCommand command, DateTimeOffset openedAt)
    {
        if (command.AlertId == Guid.Empty)
            throw new DomainException("An incident must originate from an alert.");
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("An incident must belong to an operator.");

        AlertId = command.AlertId;
        OperatorId = command.OperatorId;
        Status = IncidentStatus.Pending;
        OpenedAt = openedAt;
    }

    public void Assign(Guid supervisorId, DateTimeOffset assignedAt)
    {
        EnsureNotClosed();
        if (Status is not (IncidentStatus.Pending or IncidentStatus.Escalated))
            throw new DomainException($"Only pending or escalated incidents can be assigned; current status is {Status}.");
        if (supervisorId == Guid.Empty)
            throw new DomainException("An incident must be assigned to a supervisor.");

        AssignedSupervisorId = supervisorId;
        AssignedAt = assignedAt;
        Status = IncidentStatus.Assigned;
    }

    public void RegisterAction(IncidentAction action)
    {
        EnsureNotClosed();
        if (Status != IncidentStatus.Assigned)
            throw new DomainException($"Actions can only be registered on assigned incidents; current status is {Status}.");
        if (action.SupervisorId != AssignedSupervisorId)
            throw new DomainException("Only the assigned supervisor can register actions on this incident.");

        _actions.Add(action);
    }

    public void Escalate()
    {
        EnsureNotClosed();
        if (Status is not (IncidentStatus.Pending or IncidentStatus.Assigned))
            throw new DomainException($"Only pending or assigned incidents can be escalated; current status is {Status}.");

        Status = IncidentStatus.Escalated;
    }

    public void Close(string resolution, DateTimeOffset closedAt)
    {
        EnsureNotClosed();
        if (Status != IncidentStatus.Assigned)
            throw new DomainException($"Only assigned incidents can be closed; current status is {Status}.");
        if (_actions.Count == 0)
            throw new DomainException("At least one action must be registered before closing the incident.");
        if (string.IsNullOrWhiteSpace(resolution))
            throw new DomainException("A resolution is required to close the incident.");

        Resolution = resolution.Trim();
        ClosedAt = closedAt;
        Status = IncidentStatus.Closed;
    }

    private void EnsureNotClosed()
    {
        if (Status == IncidentStatus.Closed)
            throw new DomainException("A closed incident cannot be modified.");
    }
}
