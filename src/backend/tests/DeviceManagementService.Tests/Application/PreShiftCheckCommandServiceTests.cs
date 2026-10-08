using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Tests.Application;

public class PreShiftCheckCommandServiceTests
{
    private readonly InMemoryMonitoringDeviceRepository _deviceRepository = new();
    private readonly InMemoryPreShiftCheckRepository _checkRepository = new();
    private readonly MonitoringDeviceCommandService _deviceService;
    private readonly PreShiftCheckCommandService _service;
    private readonly PreShiftCheckQueryService _queryService;

    public PreShiftCheckCommandServiceTests()
    {
        _deviceService = new MonitoringDeviceCommandService(_deviceRepository, TimeProvider.System);
        _service = new PreShiftCheckCommandService(_deviceRepository, _checkRepository, TimeProvider.System);
        _queryService = new PreShiftCheckQueryService(_checkRepository);
    }

    private async Task AssignAsync(Guid operatorId, DeviceType type, int? batteryLevel = 90)
    {
        var device = await _deviceService.Handle(new RegisterDeviceCommand($"{type}-{Guid.NewGuid():N}", type));
        await _deviceService.Handle(new AssignDeviceCommand(device.Id, operatorId, "CAT-01"));
        await _deviceService.Handle(new ReportHeartbeatCommand(device.Id, batteryLevel));
    }

    [Fact]
    public async Task Handle_AllDevicesAssigned_PersistsFullProtectionCheck()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        foreach (var type in Enum.GetValues<DeviceType>()) await AssignAsync(operatorId, type);

        // Act
        var check = await _service.Handle(new RunPreShiftCheckCommand(operatorId, "CAT-01"));

        // Assert
        Assert.Equal(ProtectionLevel.Full, check.Protection);
        Assert.Same(check, await _queryService.Handle(new GetPreShiftCheckByIdQuery(check.Id)));
    }

    [Fact]
    public async Task Handle_DevicesOfAnotherOperator_AreIgnored()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        foreach (var type in Enum.GetValues<DeviceType>()) await AssignAsync(Guid.NewGuid(), type);

        // Act
        var check = await _service.Handle(new RunPreShiftCheckCommand(operatorId, "CAT-01"));

        // Assert
        Assert.Equal(ProtectionLevel.NotAvailable, check.Protection);
        Assert.False(check.CanStartMonitoring);
    }

    [Fact]
    public async Task Handle_TwoChecks_LatestIsReturnedForOperator()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _service.Handle(new RunPreShiftCheckCommand(operatorId, "CAT-01"));
        foreach (var type in Enum.GetValues<DeviceType>()) await AssignAsync(operatorId, type, 10);
        var second = await _service.Handle(new RunPreShiftCheckCommand(operatorId, "CAT-01"));

        // Act
        var latest = await _queryService.Handle(new GetLatestPreShiftCheckByOperatorIdQuery(operatorId));
        var none = await _queryService.Handle(new GetLatestPreShiftCheckByOperatorIdQuery(Guid.NewGuid()));

        // Assert
        Assert.Equal(second.Id, latest!.Id);
        Assert.Equal(ProtectionLevel.Limited, latest.Protection);
        Assert.Null(none);
    }
}
