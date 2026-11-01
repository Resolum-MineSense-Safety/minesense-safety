namespace FatigueDetectionService.Application.Internal.IntegrationEvents;

/// <summary>
/// Fields of <c>FatigueReadingSynchronized</c> (Edge Synchronization) that Fatigue Detection needs.
/// Local copy: bounded contexts do not share event types.
/// </summary>
public record FatigueReadingSynchronizedEvent(Guid EdgeEventId, DateTimeOffset CapturedAt, FatigueReadingData Data);

/// <summary>Payload the cabin wrote for a <c>FatigueReadingClassified</c> event.</summary>
public record FatigueReadingData(
    Guid OperatorId,
    Guid MonitoringSessionId,
    double Perclos,
    double BlinkRatePerMinute,
    double HeartRateVariabilityMs,
    string? RiskLevel);
