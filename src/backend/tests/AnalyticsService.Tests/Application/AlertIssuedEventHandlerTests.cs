using AnalyticsService.Application.Internal.CommandServices;
using AnalyticsService.Application.Internal.EventHandlers;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace AnalyticsService.Tests.Application;

public class AlertIssuedEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 22, 0, 0, TimeSpan.Zero);
    private readonly FakeFatigueMetricRepository _metrics = new();
    private readonly InMemoryOperatorContextRepository _contexts = new();
    private readonly AlertIssuedEventHandler _alertHandler;
    private readonly FleetOperatorUpdatedEventHandler _fleetHandler;

    public AlertIssuedEventHandlerTests()
    {
        _alertHandler = new AlertIssuedEventHandler(new FatigueMetricCommandService(_metrics), _contexts,
            NullLogger<AlertIssuedEventHandler>.Instance);
        _fleetHandler = new FleetOperatorUpdatedEventHandler(_contexts);
    }

    private static IntegrationEvent Issued(Guid alertId, Guid operatorId, string severity = "Critical") =>
        IntegrationEvent.Create("AlertIssued", "alert-service", alertId,
            new { alertId, operatorId, assessmentId = Guid.NewGuid(), severity, issuedAt = Now }, Now);

    private static IntegrationEvent FleetUpdated(Guid operatorId, Guid miningUnitId, DateTimeOffset updatedAt,
        string location = "Pit North") =>
        IntegrationEvent.Create("FleetOperatorUpdated", "fleet-monitoring-service", operatorId,
            new { operatorId, vehicleCode = "CAT-797-02", fleet = "Haulage", miningUnitId, fleetId = Guid.NewGuid(),
                shift = "Night", location, riskLevel = "Critical", updatedAt }, updatedAt);

    [Fact]
    public async Task HandleAsync_AlertOfKnownOperator_RecordsObservationWithFleetContext()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var miningUnitId = Guid.NewGuid();
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, miningUnitId, Now.AddHours(-1)), CancellationToken.None);
        var alertId = Guid.NewGuid();

        // Act
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Assert
        var metric = Assert.Single(_metrics.Items);
        Assert.Equal(alertId, metric.AlertId);
        Assert.Equal(FatigueSeverity.Critical, metric.Severity);
        Assert.Equal(Now, metric.OccurredAt);
        Assert.Equal(new OperationalContext("Night", miningUnitId.ToString(), "Pit North", "Haulage"), metric.Context);
    }

    [Fact]
    public async Task HandleAsync_AlertOfOperatorWithoutFleetContext_RecordsUnknownDimensions()
    {
        // Act
        await _alertHandler.HandleAsync(Issued(Guid.NewGuid(), Guid.NewGuid(), "Warning"), CancellationToken.None);

        // Assert
        var metric = Assert.Single(_metrics.Items);
        Assert.Equal(new OperationalContext("unknown", "unknown", "unknown", "unknown"), metric.Context);
    }

    [Fact]
    public async Task HandleAsync_RedeliveredAlert_RecordsOneObservation()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var operatorId = Guid.NewGuid();
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Act
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Assert
        Assert.Single(_metrics.Items);
    }

    [Fact]
    public async Task HandleAsync_OlderFleetSnapshot_DoesNotReplaceNewerContext()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, Guid.NewGuid(), Now, "Pit South"), CancellationToken.None);

        // Act
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, Guid.NewGuid(), Now.AddMinutes(-5), "Pit North"),
            CancellationToken.None);

        // Assert
        Assert.Equal("Pit South", (await _contexts.FindByOperatorIdAsync(operatorId))!.Location);
    }
}
