using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Aggregates;

// Sprint 1 · T16 (Renato Calvo): operator-shift-vehicle assignment with validity period and history (US19).
/// <summary>
/// Associates an operator with a shift and a vehicle for the period [ValidFrom, ValidTo]
/// (ValidTo null = open-ended). A vehicle change supersedes this assignment and creates a new one,
/// so the previous context stays in the history.
/// </summary>
public class OperatorAssignment : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public Guid VehicleId { get; private set; }
    public Shift Shift { get; private set; }
    public DateOnly ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public OperatorAssignment(CreateAssignmentCommand command, DateTimeOffset createdAt)
    {
        MandatoryData.EnsurePresent(
            (nameof(OperatorId), command.OperatorId == Guid.Empty),
            (nameof(VehicleId), command.VehicleId == Guid.Empty),
            (nameof(ValidFrom), command.ValidFrom == default));

        if (command.ValidTo is { } validTo && validTo < command.ValidFrom)
            throw new DomainException("ValidTo cannot be earlier than ValidFrom.");

        OperatorId = command.OperatorId;
        VehicleId = command.VehicleId;
        Shift = command.Shift;
        ValidFrom = command.ValidFrom;
        ValidTo = command.ValidTo;
        Status = AssignmentStatus.Active;
        CreatedAt = createdAt;
    }

    public bool IsActive => Status == AssignmentStatus.Active;

    public bool Covers(DateOnly date) => ValidFrom <= date && (ValidTo is null || date <= ValidTo);

    public bool Overlaps(DateOnly from, DateOnly? to) =>
        ValidFrom <= (to ?? DateOnly.MaxValue) && from <= (ValidTo ?? DateOnly.MaxValue);

    /// <summary>
    /// Supersedes this assignment and returns the new one for <paramref name="newVehicleId"/>
    /// (same operator and shift, from <paramref name="changeDate"/> to the original end).
    /// The previous assignment ends the day before the change; when the change happens on its
    /// first day it is closed that same day (ValidTo = ValidFrom) so its period never becomes empty.
    /// </summary>
    public OperatorAssignment ChangeVehicle(Guid newVehicleId, DateOnly changeDate, DateTimeOffset changedAt)
    {
        var replacement = ProposeVehicleChange(newVehicleId, changeDate, changedAt);
        Supersede(changeDate);
        return replacement;
    }

    /// <summary>
    /// Validates a vehicle change and builds the replacement assignment without modifying this one,
    /// so conflicts can be checked before anything changes.
    /// </summary>
    public OperatorAssignment ProposeVehicleChange(Guid newVehicleId, DateOnly changeDate, DateTimeOffset changedAt)
    {
        EnsureActive();
        if (newVehicleId == VehicleId)
            throw new DomainException("The new vehicle must be different from the current one.");
        if (!Covers(changeDate))
            throw new DomainException($"Change date {changeDate:yyyy-MM-dd} is outside the assignment period.");

        return new OperatorAssignment(
            new CreateAssignmentCommand(OperatorId, newVehicleId, Shift, changeDate, ValidTo), changedAt);
    }

    public void Supersede(DateOnly changeDate)
    {
        EnsureActive();
        Status = AssignmentStatus.Superseded;
        ValidTo = changeDate > ValidFrom ? changeDate.AddDays(-1) : ValidFrom;
    }

    public void End(DateOnly endDate)
    {
        EnsureActive();
        if (endDate < ValidFrom)
            throw new DomainException("End date cannot be earlier than ValidFrom.");

        Status = AssignmentStatus.Ended;
        ValidTo = endDate;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new DomainException($"Only active assignments can be modified; current status is {Status}.");
    }
}
