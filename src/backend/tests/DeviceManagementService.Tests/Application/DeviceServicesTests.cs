using DeviceManagementService.Application.Internal.CommandServices;
using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.InMemory;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Application;

public class DeviceServicesTests
{
    private readonly InMemoryDeviceRepository _repository = new();
    private readonly DeviceCommandService _commandService;
    private readonly DeviceQueryService _queryService;

    public DeviceServicesTests()
    {
        _commandService = new DeviceCommandService(_repository);
        _queryService = new DeviceQueryService(_repository);
    }

    [Fact]
    public async Task Handle_RegisterDeviceCommand_PersistsDevice()
    {
        var device = await _commandService.Handle(
            new RegisterDeviceCommand("cam-01", "camera", Guid.NewGuid()));

        Assert.Same(device, await _queryService.Handle(new GetDeviceByIdQuery(device.Id)));
    }

    [Fact]
    public async Task Handle_RegisterDuplicateCode_ThrowsDomainConflictException()
    {
        var command = new RegisterDeviceCommand("cam-01", "camera", Guid.NewGuid());
        await _commandService.Handle(command);

        await Assert.ThrowsAsync<DomainConflictException>(() =>
            _commandService.Handle(command));
    }

    [Fact]
    public async Task Handle_AssignDeviceCommand_UpdatesExistingDevice()
    {
        var device = await _commandService.Handle(
            new RegisterDeviceCommand("cam-01", "camera", Guid.NewGuid()));
        var operatorId = Guid.NewGuid();

        var assigned = await _commandService.Handle(
            new AssignDeviceCommand(device.Id, operatorId, null));

        Assert.Equal(operatorId, assigned!.AssignedOperatorId);
    }

    [Fact]
    public async Task Handle_ReportDeviceStatusCommand_UpdatesExistingDevice()
    {
        var device = await _commandService.Handle(
            new RegisterDeviceCommand("cam-01", "camera", Guid.NewGuid()));
        var reportedAt = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        var updated = await _commandService.Handle(
            new ReportDeviceStatusCommand(device.Id, DeviceStatus.Available, new BatteryLevel(95),
                reportedAt, ["camera"]));

        Assert.Equal(DeviceStatus.Available, updated!.Status);
        Assert.Equal(reportedAt, updated.ReportedAt);
    }

    [Fact]
    public async Task Handle_CommandsForUnknownDevice_ReturnNull()
    {
        var assignment = await _commandService.Handle(
            new AssignDeviceCommand(Guid.NewGuid(), Guid.NewGuid(), null));
        var status = await _commandService.Handle(
            new ReportDeviceStatusCommand(Guid.NewGuid(), DeviceStatus.Interrupted, new BatteryLevel(0),
                DateTimeOffset.UtcNow, []));

        Assert.Null(assignment);
        Assert.Null(status);
    }

    [Fact]
    public async Task Handle_GetDevicesQuery_ReturnsRegisteredDevices()
    {
        await _commandService.Handle(
            new RegisterDeviceCommand("cam-01", "camera", Guid.NewGuid()));
        await _commandService.Handle(
            new RegisterDeviceCommand("wearable-01", "wearable", Guid.NewGuid()));

        var devices = await _queryService.Handle(new GetDevicesQuery());

        Assert.Equal(2, devices.Count());
    }
}
