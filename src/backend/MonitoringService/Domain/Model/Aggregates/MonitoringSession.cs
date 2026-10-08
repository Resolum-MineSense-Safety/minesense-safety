using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Domain.Model.Aggregates;

/// <summary>
/// Monitoring of an operator during the shift (EP01 · US01/US02). It records which
/// signals are available, evaluates them continuously and reports missing information
/// instead of showing an incorrect reading.
/// </summary>
public class MonitoringSession : AggregateRoot
{
    private readonly HashSet<SignalType> _availableSignals;
    private readonly List<SignalEvent> _signalEvents = [];

    public Guid OperatorId { get; private set; }
    public string VehicleCode { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid? PreShiftCheckId { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? StoppedAt { get; private set; }

    public IReadOnlyList<SignalType> AvailableSignals =>
        _availableSignals.OrderBy(signal => signal).ToList();

    public IReadOnlyList<SignalType> MissingSignals =>
        IsOpen ? MonitoringStatusPolicy.MissingSignals(_availableSignals) : [];

    public IReadOnlyList<SignalEvent> SignalEvents => _signalEvents.AsReadOnly();

    public MonitoringStatus Status => MonitoringStatusPolicy.Evaluate(_availableSignals, !IsOpen);

    public bool IsOpen => !StoppedAt.HasValue;

    public MonitoringSession(StartMonitoringCommand command, Guid assignmentId, DateTimeOffset startedAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("A monitoring session must belong to an operator.");
        if (string.IsNullOrWhiteSpace(command.VehicleCode))
            throw new DomainException("A monitoring session must be linked to a vehicle.");
        if (assignmentId == Guid.Empty)
            throw new DomainException("A monitoring session requires a valid operator assignment.");

        OperatorId = command.OperatorId;
        VehicleCode = command.VehicleCode.Trim();
        AssignmentId = assignmentId;
        PreShiftCheckId = command.PreShiftCheckId;
        StartedAt = startedAt;
        _availableSignals = new HashSet<SignalType>(command.AvailableSignals ?? []);
    }

    public void ReportSignalLost(SignalType signal, DateTimeOffset at)
    {
        EnsureOpen();
        if (!_availableSignals.Remove(signal))
            throw new DomainException($"Signal {signal} is already unavailable.");

        _signalEvents.Add(new SignalEvent(signal, SignalEventType.Lost, at));
    }

    public void ReportSignalRestored(SignalType signal, DateTimeOffset at)
    {
        EnsureOpen();
        if (!_availableSignals.Add(signal))
            throw new DomainException($"Signal {signal} is already available.");

        _signalEvents.Add(new SignalEvent(signal, SignalEventType.Restored, at));
    }

    public void Stop(DateTimeOffset at)
    {
        if (!IsOpen)
            throw new DomainException("The monitoring session is already stopped.");

        StoppedAt = at;
    }

    private void EnsureOpen()
    {
        if (!IsOpen)
            throw new DomainException("The monitoring session is stopped; signals can no longer be reported.");
    }
}
