using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Tests.Infrastructure;

public class InMemoryDeviceRepositoryTests
{
    private readonly InMemoryDeviceRepository _repository = new();

    private static Device Device(string code) =>
        new(new RegisterDeviceCommand(code, "camera", Guid.NewGuid()));

    [Fact]
    public async Task AddAndUpdate_StoreTheDevice()
    {
        var device = Device("cam-01");

        await _repository.AddAsync(device);
        device.ReportStatus(new ReportDeviceStatusCommand(
            device.Id,
            DeviceStatus.Available,
            new BatteryLevel(90),
            DateTimeOffset.UtcNow,
            ["camera"]));
        await _repository.UpdateAsync(device);

        Assert.Same(device, await _repository.FindByIdAsync(device.Id));
        Assert.Equal(DeviceStatus.Available,
            (await _repository.FindByIdAsync(device.Id))!.Status);
    }

    [Fact]
    public async Task FindByIdAndList_ReturnStoredDevices()
    {
        var first = Device("cam-01");
        var second = Device("wearable-01");
        await _repository.AddAsync(first);
        await _repository.AddAsync(second);

        var devices = (await _repository.ListAsync()).ToList();

        Assert.Equal(2, devices.Count);
        Assert.Same(first, await _repository.FindByIdAsync(first.Id));
        Assert.Null(await _repository.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindByDeviceCodeAsync_ReturnsMatchingDevice()
    {
        var device = Device("cam-01");
        await _repository.AddAsync(device);
        await _repository.AddAsync(Device("wearable-01"));

        Assert.Same(device, await _repository.FindByDeviceCodeAsync("cam-01"));
        Assert.Null(await _repository.FindByDeviceCodeAsync("missing"));
    }
}
