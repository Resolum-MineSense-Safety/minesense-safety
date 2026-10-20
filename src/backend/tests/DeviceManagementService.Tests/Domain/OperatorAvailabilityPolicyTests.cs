using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Services;

namespace DeviceManagementService.Tests.Domain;

public class OperatorAvailabilityPolicyTests
{
    private readonly Guid _operatorId = Guid.NewGuid();

    [Fact]
    public void Evaluate_WithAllDevicesAvailable_ReportsActiveMonitoring()
    {
        var devices = new[]
        {
            CreateDevice(_operatorId, DeviceStatus.Available),
            CreateDevice(_operatorId, DeviceStatus.Available)
        };

        var result = Assert.Single(OperatorAvailabilityPolicy.Evaluate(devices));

        Assert.Equal(_operatorId, result.OperatorId);
        Assert.Equal(OperatorMonitoringStatus.Active, result.Status);
        Assert.Equal(2, result.Devices.Count);
    }

    [Fact]
    public void Evaluate_WithOneInterruptedDevice_ReportsPartialMonitoring()
    {
        var devices = new[]
        {
            CreateDevice(_operatorId, DeviceStatus.Available),
            CreateDevice(_operatorId, DeviceStatus.Interrupted)
        };

        var result = Assert.Single(OperatorAvailabilityPolicy.Evaluate(devices));

        Assert.Equal(OperatorMonitoringStatus.Partial, result.Status);
    }

    [Fact]
    public void Evaluate_WithoutAvailableDevices_ReportsUnavailableMonitoring()
    {
        var devices = new[]
        {
            CreateDevice(_operatorId, DeviceStatus.Interrupted),
            CreateDevice(_operatorId, null)
        };

        var result = Assert.Single(OperatorAvailabilityPolicy.Evaluate(devices));

        Assert.Equal(OperatorMonitoringStatus.Unavailable, result.Status);
    }

    [Fact]
    public void Evaluate_WithRecoveredDevice_ReportsActiveMonitoringAgain()
    {
        var device = CreateDevice(_operatorId, DeviceStatus.Interrupted);
        ReportStatus(device, DeviceStatus.Available);

        var result = Assert.Single(OperatorAvailabilityPolicy.Evaluate([device]));

        Assert.Equal(OperatorMonitoringStatus.Active, result.Status);
    }

    [Fact]
    public void Evaluate_GroupsDevicesByOperatorAndIgnoresUnassigned()
    {
        var otherOperatorId = Guid.NewGuid();
        var devices = new[]
        {
            CreateDevice(_operatorId, DeviceStatus.Available),
            CreateDevice(otherOperatorId, DeviceStatus.Interrupted),
            CreateDevice(null, DeviceStatus.Available)
        };

        var result = OperatorAvailabilityPolicy.Evaluate(devices);

        Assert.Equal(2, result.Count);
        Assert.Equal(OperatorMonitoringStatus.Active,
            result.Single(availability => availability.OperatorId == _operatorId).Status);
        Assert.Equal(OperatorMonitoringStatus.Unavailable,
            result.Single(availability => availability.OperatorId == otherOperatorId).Status);
    }

    private static Device CreateDevice(Guid? operatorId, DeviceStatus? status)
    {
        var device = new Device(new RegisterDeviceCommand(
            $"camera-{Guid.NewGuid():N}", "camera", Guid.NewGuid()));
        if (operatorId is not null)
            device.Assign(new AssignDeviceCommand(device.Id, operatorId, null));
        if (status is not null)
            ReportStatus(device, status.Value);
        return device;
    }

    private static void ReportStatus(Device device, DeviceStatus status) =>
        device.ReportStatus(new ReportDeviceStatusCommand(
            device.Id, status, new BatteryLevel(80), DateTimeOffset.UtcNow, []));
}
