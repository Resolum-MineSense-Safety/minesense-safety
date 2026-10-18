using DeviceManagementService.Application.Internal.QueryServices;
using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Infrastructure.Persistence.InMemory;

namespace DeviceManagementService.Tests.Application;

public class PreShiftAvailabilityQueryServiceTests
{
    private readonly InMemoryDeviceRepository _repository = new();
    private readonly PreShiftAvailabilityQueryService _service;
    private readonly BatteryThreshold _batteryThreshold = new(50);

    public PreShiftAvailabilityQueryServiceTests()
    {
        _service = new PreShiftAvailabilityQueryService(_repository);
    }

    [Fact]
    public async Task Handle_WithAvailableAssignedCamera_AllowsMonitoring()
    {
        var operatorId = Guid.NewGuid();
        await AddCameraAsync(operatorId, DeviceStatus.Available, 80);

        var result = await _service.Handle(
            new GetPreShiftAvailabilityQuery(operatorId), _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Available, result.Status);
        Assert.Equal(PreShiftRecommendation.StartMonitoring, result.Recommendation);
    }

    [Fact]
    public async Task Handle_WithLowBatteryAssignedCamera_ReturnsWarning()
    {
        var operatorId = Guid.NewGuid();
        await AddCameraAsync(operatorId, DeviceStatus.Available, 25);

        var result = await _service.Handle(
            new GetPreShiftAvailabilityQuery(operatorId), _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.Warning, result.Status);
        Assert.Equal(PreShiftRecommendation.ReviewBatteryBeforeStarting, result.Recommendation);
    }

    [Fact]
    public async Task Handle_WithoutAssignedCamera_ReportsIncompleteProtection()
    {
        var result = await _service.Handle(
            new GetPreShiftAvailabilityQuery(Guid.NewGuid()), _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Equal(PreShiftRecommendation.DoNotStartMonitoring, result.Recommendation);
        Assert.Empty(result.EvaluatedDevices);
    }

    [Fact]
    public async Task Handle_WithDevicesAssignedToAnotherOperator_IgnoresThem()
    {
        var requestedOperatorId = Guid.NewGuid();
        await AddCameraAsync(Guid.NewGuid(), DeviceStatus.Available, 90);

        var result = await _service.Handle(
            new GetPreShiftAvailabilityQuery(requestedOperatorId), _batteryThreshold);

        Assert.Equal(PreShiftAvailabilityStatus.ProtectionIncomplete, result.Status);
        Assert.Empty(result.EvaluatedDevices);
    }

    private async Task<Device> AddCameraAsync(
        Guid operatorId, DeviceStatus status, decimal battery)
    {
        var device = new Device(new RegisterDeviceCommand(
            $"camera-{Guid.NewGuid():N}", "camera", Guid.NewGuid()));
        device.Assign(new AssignDeviceCommand(device.Id, operatorId, null));
        device.ReportStatus(new ReportDeviceStatusCommand(
            device.Id, status, new BatteryLevel(battery), DateTimeOffset.UtcNow, []));
        await _repository.AddAsync(device);
        return device;
    }
}
