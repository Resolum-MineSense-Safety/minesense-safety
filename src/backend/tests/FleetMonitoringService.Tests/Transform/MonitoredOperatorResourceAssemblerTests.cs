using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Interfaces.REST.Transform;

namespace FleetMonitoringService.Tests.Transform;

public class MonitoredOperatorResourceAssemblerTests
{
    private static readonly DateTimeOffset RegisteredAt =
        new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToResourceFromEntity_AfterThreshold_ExposesStaleStatus()
    {
        // Arrange
        var monitoredOperator = new MonitoredOperator(
            new RegisterMonitoredOperatorCommand(
                Guid.NewGuid(), "Ana Rojas", "CAT-797-01", "Haulage", Shift.Night, "Pit North"),
            RegisteredAt);

        // Act
        var resource = MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(
            monitoredOperator, RegisteredAt.AddMinutes(6), TimeSpan.FromMinutes(5));

        // Assert
        Assert.Equal("Stale", resource.Freshness);
        Assert.True(resource.IsStale);
    }
}
