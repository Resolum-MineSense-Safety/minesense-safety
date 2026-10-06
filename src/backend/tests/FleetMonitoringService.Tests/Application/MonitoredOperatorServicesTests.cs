using FleetMonitoringService.Application.Internal.CommandServices;
using FleetMonitoringService.Application.Internal.QueryServices;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.Queries;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Tests.Application;

public class MonitoredOperatorServicesTests
{
    private readonly InMemoryMonitoredOperatorRepository _repository = new();
    private readonly MonitoredOperatorCommandService _commandService;
    private readonly MonitoredOperatorQueryService _queryService;

    public MonitoredOperatorServicesTests()
    {
        _commandService = new MonitoredOperatorCommandService(_repository, TimeProvider.System);
        _queryService = new MonitoredOperatorQueryService(_repository);
    }

    private async Task<Guid> RegisterAsync(string fullName, string fleet, Shift shift, string location,
        RiskLevel riskLevel = RiskLevel.Normal)
    {
        var operatorId = Guid.NewGuid();
        await _commandService.Handle(
            new RegisterMonitoredOperatorCommand(operatorId, fullName, "CAT-797-01", fleet, shift, location));
        await _commandService.Handle(new UpdateOperatorRiskLevelCommand(operatorId, riskLevel));
        return operatorId;
    }

    [Fact]
    public async Task Handle_RegisterDuplicatedOperator_ThrowsDomainException()
    {
        // Arrange
        var command = new RegisterMonitoredOperatorCommand(
            Guid.NewGuid(), "Juan Perez", "CAT-797-01", "Haulage", Shift.Day, "Pit North");
        await _commandService.Handle(command);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => _commandService.Handle(command));
    }

    [Fact]
    public async Task Handle_UpdateRiskOfUnknownOperator_ReturnsNull()
    {
        // Arrange
        var command = new UpdateOperatorRiskLevelCommand(Guid.NewGuid(), RiskLevel.Warning);

        // Act
        var monitoredOperator = await _commandService.Handle(command);

        // Assert
        Assert.Null(monitoredOperator);
    }

    [Fact]
    public async Task Handle_GetFleetStatusWithCombinedCriteria_ReturnsOnlyMatches()
    {
        // Arrange
        var expected = await RegisterAsync("Ana Rojas", "Haulage", Shift.Night, "Pit North", RiskLevel.Critical);
        await RegisterAsync("Luis Diaz", "Haulage", Shift.Day, "Pit North", RiskLevel.Critical);
        await RegisterAsync("Rosa Vega", "Loading", Shift.Night, "Pit North", RiskLevel.Critical);
        await RegisterAsync("Mario Cruz", "Haulage", Shift.Night, "Pit North", RiskLevel.Warning);
        var query = new GetFleetStatusQuery(Shift.Night, "haulage", "Pit North", RiskLevel.Critical);

        // Act
        var result = (await _queryService.Handle(query)).ToList();

        // Assert
        var match = Assert.Single(result);
        Assert.Equal(expected, match.OperatorId);
    }

    [Fact]
    public async Task Handle_GetFleetStatusWithoutCriteria_OrdersByRiskThenName()
    {
        // Arrange
        await RegisterAsync("Zoe Lima", "Haulage", Shift.Day, "Pit North", RiskLevel.Normal);
        await RegisterAsync("Bruno Paz", "Haulage", Shift.Day, "Pit North", RiskLevel.Critical);
        await RegisterAsync("Carla Soto", "Haulage", Shift.Day, "Pit North", RiskLevel.Warning);
        await RegisterAsync("Abel Ruiz", "Haulage", Shift.Day, "Pit North", RiskLevel.Critical);

        // Act
        var result = await _queryService.Handle(new GetFleetStatusQuery());

        // Assert
        Assert.Equal(
            ["Abel Ruiz", "Bruno Paz", "Carla Soto", "Zoe Lima"],
            result.Select(monitoredOperator => monitoredOperator.FullName));
    }
}
