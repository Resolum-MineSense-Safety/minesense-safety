using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Interfaces.REST.Resources;

namespace MonitoringService.Interfaces.REST.Transform;

// Sprint 1 · T08 (Renato Calvo): builds the status shown to the operator with the Spanish message of US02.
public static class MonitoringStatusResourceFromEntityAssembler
{
    public static MonitoringStatusResource ToResourceFromEntity(MonitoringSession session) =>
        new(session.Id,
            session.OperatorId,
            session.VehicleCode,
            session.Status.ToString(),
            session.AvailableSignals.Select(signal => signal.ToString()).ToList(),
            session.MissingSignals.Select(signal => signal.ToString()).ToList(),
            ToOperatorMessage(session.Status, session.MissingSignals),
            session.StartedAt,
            session.StoppedAt);

    public static string ToOperatorMessage(MonitoringStatus status, IEnumerable<SignalType> missingSignals) =>
        status switch
        {
            MonitoringStatus.Active => "Monitoreo activo",
            MonitoringStatus.Partial =>
                $"Monitoreo parcial: falta {string.Join(", ", missingSignals.Select(ToSpanishName))}",
            MonitoringStatus.Unavailable => "Monitoreo no disponible",
            _ => "Monitoreo detenido"
        };

    public static string ToSpanishName(SignalType signal) => signal switch
    {
        SignalType.EyeTracking => "seguimiento ocular (cámara)",
        SignalType.HeartRate => "frecuencia cardiaca (wearable)",
        _ => signal.ToString()
    };
}
