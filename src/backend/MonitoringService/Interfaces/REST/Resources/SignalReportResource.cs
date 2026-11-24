namespace MonitoringService.Interfaces.REST.Resources;

/// <summary>
/// Body sent by the Edge gateway when a signal is lost or restored (EyeTracking | HeartRate).
/// </summary>
public record SignalReportResource(string Signal);
