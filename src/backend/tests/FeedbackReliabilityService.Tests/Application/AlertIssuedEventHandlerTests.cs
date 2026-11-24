using FeedbackReliabilityService.Application.Internal.EventHandlers;
using FeedbackReliabilityService.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FeedbackReliabilityService.Tests.Application;

public class AlertIssuedEventHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 22, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAlertEvaluationRepository _repository = new();
    private readonly InMemoryOperatorOperationDirectory _directory = new();
    private readonly AlertIssuedEventHandler _alertHandler;
    private readonly FleetOperatorUpdatedEventHandler _fleetHandler;

    public AlertIssuedEventHandlerTests()
    {
        _alertHandler = new AlertIssuedEventHandler(_repository, _directory, NullLogger<AlertIssuedEventHandler>.Instance);
        _fleetHandler = new FleetOperatorUpdatedEventHandler(_directory);
    }

    private static IntegrationEvent Issued(Guid alertId, Guid operatorId) =>
        IntegrationEvent.Create("AlertIssued", "alert-service", alertId,
            new { alertId, operatorId, assessmentId = Guid.NewGuid(), severity = "Warning", issuedAt = Now }, Now);

    private static IntegrationEvent FleetUpdated(Guid operatorId, Guid? miningUnitId) =>
        IntegrationEvent.Create("FleetOperatorUpdated", "fleet-monitoring-service", operatorId,
            new { operatorId, miningUnitId, shift = "Night", updatedAt = Now.AddHours(-1) }, Now.AddHours(-1));

    [Fact]
    public async Task HandleAsync_AlertOfOperatorWithOperation_TracksAlertUnderThatOperation()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, operationId), CancellationToken.None);
        var alertId = Guid.NewGuid();

        // Act
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Assert
        var evaluation = await _repository.FindAsync(alertId);
        Assert.NotNull(evaluation);
        Assert.Equal(operationId, evaluation.OperationId);
        Assert.Equal(operatorId, evaluation.OperatorId);
        Assert.Equal(Now, evaluation.IssuedAt);
        Assert.Equal("PENDING", evaluation.Status);
    }

    [Fact]
    public async Task HandleAsync_AlertOfOperatorWithoutOperation_IsNotTracked()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, null), CancellationToken.None);
        var alertId = Guid.NewGuid();

        // Act
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Assert
        Assert.Null(await _repository.FindAsync(alertId));
    }

    [Fact]
    public async Task HandleAsync_RedeliveredAlert_KeepsTheOriginalEvaluation()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _fleetHandler.HandleAsync(FleetUpdated(operatorId, Guid.NewGuid()), CancellationToken.None);
        var alertId = Guid.NewGuid();
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);
        var first = await _repository.FindAsync(alertId);

        // Act
        await _alertHandler.HandleAsync(Issued(alertId, operatorId), CancellationToken.None);

        // Assert
        Assert.Same(first, await _repository.FindAsync(alertId));
    }
}
