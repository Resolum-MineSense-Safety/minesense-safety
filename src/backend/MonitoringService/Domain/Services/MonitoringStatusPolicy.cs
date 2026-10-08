using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Services;

// Sprint 1 · T07 (Nicolas Juarez): status indicator rules (Active / Partial / Unavailable / Stopped). Remaining: agree on the visual representation with the dashboard (T09).
/// <summary>
/// Domain policy that turns the set of available signals into the monitoring status
/// shown to the operator (US02): both signals -> Active, one signal -> Partial,
/// none -> Unavailable, finished session -> Stopped.
/// </summary>
public static class MonitoringStatusPolicy
{
    public static readonly IReadOnlyList<SignalType> RequiredSignals = Enum.GetValues<SignalType>();

    public static MonitoringStatus Evaluate(IEnumerable<SignalType> availableSignals, bool stopped)
    {
        if (stopped) return MonitoringStatus.Stopped;

        var available = availableSignals.Distinct().Count(RequiredSignals.Contains);
        if (available == 0) return MonitoringStatus.Unavailable;
        return available == RequiredSignals.Count ? MonitoringStatus.Active : MonitoringStatus.Partial;
    }

    public static IReadOnlyList<SignalType> MissingSignals(IEnumerable<SignalType> availableSignals)
    {
        var available = availableSignals.ToHashSet();
        return RequiredSignals.Where(signal => !available.Contains(signal)).ToList();
    }
}
