namespace FleetMonitoringService.Application.Internal.Configuration;

public sealed class FleetMonitoringOptions
{
    public const string SectionName = "FleetMonitoring";

    public int StaleAfterMinutes { get; init; } = 5;

    public TimeSpan StaleAfter => TimeSpan.FromMinutes(StaleAfterMinutes);
}
