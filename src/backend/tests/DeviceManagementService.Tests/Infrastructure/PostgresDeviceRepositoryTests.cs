using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.PostgreSql;
using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;

namespace DeviceManagementService.Tests.Infrastructure;

public class PostgresDeviceRepositoryTests
{
    private static readonly DateTimeOffset ReportedAt = new(2026, 10, 6, 6, 30, 0, 123, TimeSpan.FromHours(-5));

    private static Device Device(string code) => new(new RegisterDeviceCommand(code, "camera", Guid.NewGuid()));

    private static void Report(Device device, decimal battery) =>
        device.ReportStatus(new ReportDeviceStatusCommand(device.Id, DeviceStatus.Limited, new BatteryLevel(battery),
            ReportedAt.AddTicks(3), ["camera", "infrared"]));

    [Fact]
    public void Snapshot_RoundTrip_RestoresBatteryComponentsAndAssignment()
    {
        // Arrange
        var device = Device("cam-01");
        device.Assign(new AssignDeviceCommand(device.Id, Guid.NewGuid(), null));
        Report(device, 18.5m);

        // Act
        var restored = AggregateSnapshot.Deserialize<Device>(AggregateSnapshot.Serialize(device));

        // Assert
        Assert.Equal(device.AssignedOperatorId, restored.AssignedOperatorId);
        Assert.Null(restored.AssignedVehicleId);
        Assert.Equal(18.5m, restored.BatteryLevel!.Value.Percentage);
        Assert.Equal(DeviceStatus.Limited, restored.Status);
        Assert.Equal(ReportedAt.AddTicks(3), restored.ReportedAt);
        Assert.Equal(new[] { "camera", "infrared" }, restored.AvailableComponents);
    }

    [PostgresFact]
    public async Task Devices_RealServer_PersistAcrossRestartFilterByOperatorAndKeepCodesUnique()
    {
        // Arrange
        await using var postgres = new PostgresTestDatabase("devices_tests");
        var repository = await postgres.PrepareAsync(new PostgresDeviceRepository(postgres.DataSource));
        var operatorId = Guid.NewGuid();
        var camera = Device("cam-01");
        var wearable = Device("wear-01");
        var unassigned = Device("cam-02");

        // Act
        await repository.AddAsync(camera);
        await repository.AddAsync(wearable);
        await repository.AddAsync(unassigned);
        camera.Assign(new AssignDeviceCommand(camera.Id, operatorId, null));
        Report(camera, 42m);
        await repository.UpdateAsync(camera);
        wearable.Assign(new AssignDeviceCommand(wearable.Id, operatorId, Guid.NewGuid()));
        await repository.UpdateAsync(wearable);
        var duplicate = await Assert.ThrowsAsync<DomainConflictException>(() => repository.AddAsync(Device(" cam-01 ")));
        var restarted = new PostgresDeviceRepository(postgres.Reopen());

        // Assert
        Assert.NotNull(duplicate);
        var stored = (await restarted.FindByDeviceCodeAsync("cam-01"))!;
        Assert.Equal(camera.Id, stored.Id);
        Assert.Equal(42m, stored.BatteryLevel!.Value.Percentage);
        Assert.Equal(new[] { "camera", "infrared" }, stored.AvailableComponents);
        Assert.Equal(new[] { camera.Id, wearable.Id },
            (await restarted.FindByAssignedOperatorIdAsync(operatorId)).Select(device => device.Id));
        Assert.Empty(await restarted.FindByAssignedOperatorIdAsync(Guid.NewGuid()));
        Assert.Null(await restarted.FindByDeviceCodeAsync("cam-99"));
        Assert.Equal(3, (await restarted.ListAsync()).Count());
    }
}
