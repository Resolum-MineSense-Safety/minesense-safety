using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Application;

public class MonitoringDeviceCommandServiceTests
{
    private readonly InMemoryMonitoringDeviceRepository _repository = new();
    private readonly MonitoringDeviceCommandService _service;
    private readonly MonitoringDeviceQueryService _queryService;

    public MonitoringDeviceCommandServiceTests()
    {
        _service = new MonitoringDeviceCommandService(_repository, TimeProvider.System);
        _queryService = new MonitoringDeviceQueryService(_repository);
    }

    [Fact]
    public async Task Handle_RegisterDeviceCommand_PersistsDevice()
    {
        // Arrange
        var command = new RegisterDeviceCommand("CAM-100", DeviceType.Camera);

        // Act
        var device = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(device.Id));
    }

    [Fact]
    public async Task Handle_RegisterDuplicatedSerialNumber_ThrowsDomainException()
    {
        // Arrange
        await _service.Handle(new RegisterDeviceCommand("CAM-100", DeviceType.Camera));

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.Handle(new RegisterDeviceCommand("cam-100 ", DeviceType.Camera)));
    }

    [Fact]
    public async Task Handle_AssignHeartbeatDisconnectionRecovery_UpdatesDevice()
    {
        // Arrange
        var device = await _service.Handle(new RegisterDeviceCommand("WR-1", DeviceType.Wearable));
        var operatorId = Guid.NewGuid();

        // Act
        await _service.Handle(new AssignDeviceCommand(device.Id, operatorId, "CAT-01"));
        await _service.Handle(new ReportHeartbeatCommand(device.Id, 90));
        await _service.Handle(new ReportDisconnectionCommand(device.Id, "Signal lost"));
        var recovered = await _service.Handle(new ReportRecoveryCommand(device.Id));

        // Assert
        Assert.Equal(operatorId, recovered!.AssignedOperatorId);
        Assert.Equal(DeviceStatus.Available, recovered.Status);
        Assert.NotNull(Assert.Single(recovered.Failures).RecoveredAt);
    }

    [Fact]
    public async Task Handle_CommandsOnUnknownDevice_ReturnNull()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var assigned = await _service.Handle(new AssignDeviceCommand(unknownId, Guid.NewGuid(), "CAT-01"));
        var heartbeat = await _service.Handle(new ReportHeartbeatCommand(unknownId, 50));
        var disconnection = await _service.Handle(new ReportDisconnectionCommand(unknownId, null));
        var recovery = await _service.Handle(new ReportRecoveryCommand(unknownId));

        // Assert
        Assert.Null(assigned);
        Assert.Null(heartbeat);
        Assert.Null(disconnection);
        Assert.Null(recovery);
    }

    [Fact]
    public async Task Queries_ByOperatorAndStatus_ReturnMatchingDevices()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var camera = await _service.Handle(new RegisterDeviceCommand("CAM-1", DeviceType.Camera));
        var alarm = await _service.Handle(new RegisterDeviceCommand("VA-1", DeviceType.VibrationAlarm));
        await _service.Handle(new AssignDeviceCommand(camera.Id, operatorId, "CAT-01"));
        await _service.Handle(new ReportDisconnectionCommand(alarm.Id, null));

        // Act
        var assigned = await _queryService.Handle(new GetDevicesByOperatorIdQuery(operatorId));
        var disconnected = await _queryService.Handle(new GetDeviceAvailabilityQuery(DeviceStatus.Disconnected));
        var all = await _queryService.Handle(new GetDeviceAvailabilityQuery());
        var byId = await _queryService.Handle(new GetDeviceByIdQuery(camera.Id));

        // Assert
        Assert.Equal(camera.Id, Assert.Single(assigned).Id);
        Assert.Equal(alarm.Id, Assert.Single(disconnected).Id);
        Assert.Equal(2, all.Count());
        Assert.Same(camera, byId);
    }
}
