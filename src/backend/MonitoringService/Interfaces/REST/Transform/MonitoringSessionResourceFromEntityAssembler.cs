using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Interfaces.REST.Resources;

namespace MonitoringService.Interfaces.REST.Transform;

public static class MonitoringSessionResourceFromEntityAssembler
{
    public static MonitoringSessionResource ToResourceFromEntity(MonitoringSession session) =>
        new(session.Id,
            session.OperatorId,
            session.VehicleCode,
            session.AssignmentId,
            session.PreShiftCheckId,
            session.Status.ToString(),
            session.AvailableSignals.Select(signal => signal.ToString()).ToList(),
            session.MissingSignals.Select(signal => signal.ToString()).ToList(),
            session.StartedAt,
            session.StoppedAt,
            session.SignalEvents
                .Select(signalEvent => new SignalEventResource(
                    signalEvent.Signal.ToString(), signalEvent.Type.ToString(), signalEvent.OccurredAt))
                .ToList());
}
