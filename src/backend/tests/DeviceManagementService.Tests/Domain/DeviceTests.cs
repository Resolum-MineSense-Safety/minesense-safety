using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Tests.Domain;

public class DeviceTests
{
    private static readonly Guid OrganizationId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidCommand_RegistersDevice()
    {
        var device = new Device(new RegisterDeviceCommand(" cam-01 ", "camera", OrganizationId));

        Assert.NotEqual(Guid.Empty, device.Id);
        Assert.Equal("cam-01", device.DeviceCode);
        Assert.Equal("camera", device.DeviceType);
        Assert.Equal(OrganizationId, device.OrganizationId);
        Assert.Null(device.Status);
    }

    [Theory]
    [InlineData("", "camera")]
    [InlineData("cam-01", "")]
    public void Constructor_WithMissingRequiredText_ThrowsDomainException(string code, string type)
    {
        Assert.Throws<DomainException>(() =>
            new Device(new RegisterDeviceCommand(code, type, OrganizationId)));
    }

    [Fact]
    public void Constructor_WithoutOrganization_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Device(new RegisterDeviceCommand("cam-01", "camera", Guid.Empty)));
    }

    [Fact]
    public void Assign_WithOperatorOrVehicle_StoresAssignment()
    {
        var device = new Device(new RegisterDeviceCommand("cam-01", "camera", OrganizationId));
        var operatorId = Guid.NewGuid();

        device.Assign(new AssignDeviceCommand(device.Id, operatorId, null));

        Assert.Equal(operatorId, device.AssignedOperatorId);
        Assert.Null(device.AssignedVehicleId);
    }

    [Fact]
    public void Assign_WithoutOperatorOrVehicle_ThrowsDomainException()
    {
        var device = new Device(new RegisterDeviceCommand("cam-01", "camera", OrganizationId));

        Assert.Throws<DomainException>(() =>
            device.Assign(new AssignDeviceCommand(device.Id, null, null)));
    }

    [Fact]
    public void Assign_WhenAlreadyAssigned_ThrowsDomainException()
    {
        var device = new Device(new RegisterDeviceCommand("cam-01", "camera", OrganizationId));
        device.Assign(new AssignDeviceCommand(device.Id, Guid.NewGuid(), null));

        Assert.Throws<DomainException>(() =>
            device.Assign(new AssignDeviceCommand(device.Id, null, Guid.NewGuid())));
    }

    [Fact]
    public void ReportStatus_StoresStatusBatteryTimestampAndComponents()
    {
        var device = new Device(new RegisterDeviceCommand("cam-01", "camera", OrganizationId));
        var reportedAt = new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);

        device.ReportStatus(new ReportDeviceStatusCommand(
            DeviceStatus.Limited,
            new BatteryLevel(75),
            reportedAt,
            ["camera", "camera", "wearable"]));

        Assert.Equal(DeviceStatus.Limited, device.Status);
        Assert.Equal(new BatteryLevel(75), device.BatteryLevel);
        Assert.Equal(reportedAt, device.ReportedAt);
        Assert.Equal(["camera", "wearable"], device.AvailableComponents);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void BatteryLevel_WithPercentageOutsideRange_ThrowsDomainException(decimal percentage)
    {
        Assert.Throws<DomainException>(() => new BatteryLevel(percentage));
    }
}
