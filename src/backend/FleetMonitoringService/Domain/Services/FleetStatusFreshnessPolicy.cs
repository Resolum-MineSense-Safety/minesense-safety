using FleetMonitoringService.Domain.Model.ValueObjects;

namespace FleetMonitoringService.Domain.Services;

public static class FleetStatusFreshnessPolicy
{
    public static FreshnessStatus Evaluate(
        DateTimeOffset lastUpdatedAt,
        DateTimeOffset now,
        TimeSpan staleAfter)
    {
        if (staleAfter <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(staleAfter), "The stale threshold must be positive.");

        return now - lastUpdatedAt > staleAfter
            ? FreshnessStatus.Stale
            : FreshnessStatus.Current;
    }
}
