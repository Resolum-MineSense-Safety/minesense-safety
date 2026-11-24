namespace MonitoringService.Domain.Model.ValueObjects;

/// <summary>
/// Signals the cabin devices provide during the shift.
/// </summary>
public enum SignalType
{
    /// <summary>Eye tracking captured by the cabin camera.</summary>
    EyeTracking,

    /// <summary>Heart rate captured by the operator wearable.</summary>
    HeartRate
}
