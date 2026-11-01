using FleetMonitoringService.Application.Internal.CommandServices;
using FleetMonitoringService.Application.Internal.EventHandlers;
using FleetMonitoringService.Application.Internal.IntegrationEvents;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Infrastructure.Persistence.InMemory;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FleetMonitoringService.Tests.Application;

public class FatigueAssessedEventHandlerTests
{
    private readonly InMemoryMonitoredOperatorRepository _repository = new();
    private readonly FakeIntegrationEventPublisher _publisher = new();
    private readonly MonitoredOperatorCommandService _commandService;
    private readonly FatigueAssessedEventHandler _handler;

    public FatigueAssessedEventHandlerTests()
    {
        _commandService = new MonitoredOperatorCommandService(_repository, TimeProvider.System, _publisher);
        _handler = new FatigueAssessedEventHandler(_commandService, NullLogger<FatigueAssessedEventHandler>.Instance);
    }

    private static IntegrationEvent Assessed(Guid operatorId, string riskLevel) =>
        IntegrationEvent.Create("FatigueAssessed", "fatigue-detection-service", Guid.NewGuid(),
            new { assessmentId = Guid.NewGuid(), operatorId, riskLevel, requiresAlert = riskLevel != "Normal",
                assessedAt = DateTimeOffset.UtcNow }, DateTimeOffset.UtcNow);

    [Fact]
    public async Task HandleAsync_AssessmentOfMonitoredOperator_UpdatesRiskAndPublishesSnapshot()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var miningUnitId = Guid.NewGuid();
        await _commandService.Handle(new RegisterMonitoredOperatorCommand(operatorId, "Rosa Quispe", "CAT-797-02",
            "Haulage", Shift.Night, "Pit North", miningUnitId, Guid.NewGuid()));

        // Act
        await _handler.HandleAsync(Assessed(operatorId, "Critical"), CancellationToken.None);

        // Assert
        var monitored = await _repository.FindByOperatorIdAsync(operatorId);
        Assert.Equal(RiskLevel.Critical, monitored!.CurrentRiskLevel);
        var (topic, published, _) = _publisher.Published[^1];
        Assert.Equal(IntegrationEventTopics.FleetOperatorUpdated, topic);
        var snapshot = published.GetPayload<FleetOperatorUpdatedEvent>();
        Assert.Equal("Critical", snapshot.RiskLevel);
        Assert.Equal("Night", snapshot.Shift);
        Assert.Equal(miningUnitId, snapshot.MiningUnitId);
    }

    [Fact]
    public async Task HandleAsync_AssessmentOfUnknownOperator_IsIgnored()
    {
        // Act
        await _handler.HandleAsync(Assessed(Guid.NewGuid(), "Warning"), CancellationToken.None);

        // Assert
        Assert.Empty(await _repository.ListAsync());
        Assert.Empty(_publisher.Published);
    }
}
