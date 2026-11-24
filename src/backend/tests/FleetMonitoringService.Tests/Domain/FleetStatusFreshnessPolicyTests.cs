using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Domain.Services;

namespace FleetMonitoringService.Tests.Domain;

public class FleetStatusFreshnessPolicyTests
{
    private static readonly DateTimeOffset LastUpdatedAt =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Evaluate_AtFreshnessBoundary_ReturnsCurrent()
    {
        // Act
        var result = FleetStatusFreshnessPolicy.Evaluate(
            LastUpdatedAt, LastUpdatedAt.AddMinutes(5), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(FreshnessStatus.Current, result);
    }

    [Fact]
    public void Evaluate_AfterFreshnessBoundary_ReturnsStale()
    {
        // Act
        var result = FleetStatusFreshnessPolicy.Evaluate(
            LastUpdatedAt, LastUpdatedAt.AddMinutes(5).AddTicks(1), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal(FreshnessStatus.Stale, result);
    }
}
