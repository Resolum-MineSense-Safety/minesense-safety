using System.Net;
using System.Net.Http.Json;
using DeviceManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DeviceManagementService.Tests.Interfaces;

public class DevicesApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<DeviceResource> RegisterDeviceAsync(string type = "Wearable")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource($"{type}-{Guid.NewGuid():N}", type));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<DeviceResource>())!;
    }

    [Fact]
    public async Task PostDevice_WithValidResource_ReturnsCreatedDevice()
    {
        // Arrange
        var serialNumber = $"CAM-{Guid.NewGuid():N}";

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/devices", new RegisterDeviceResource(serialNumber, "camera"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var device = await response.Content.ReadFromJsonAsync<DeviceResource>();
        Assert.Equal(serialNumber, device!.SerialNumber);
        Assert.Equal("Camera", device.Type);
        Assert.Equal("Available", device.Status);
        Assert.Empty(device.Failures);
    }

    [Theory]
    [InlineData("Toaster")]
    [InlineData("7")]
    public async Task PostDevice_WithUnknownType_ReturnsUnprocessableEntity(string type)
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/devices", new RegisterDeviceResource("X-1", type));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostDevice_WithDuplicatedSerialNumber_ReturnsUnprocessableEntity()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource(device.SerialNumber, "Wearable"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetDeviceById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var found = await _client.GetAsync($"/api/v1/devices/{device.Id}");
        var missing = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PutAssignment_ThenGetByOperator_ReturnsAssignedDevice()
    {
        // Arrange
        var device = await RegisterDeviceAsync();
        await RegisterDeviceAsync();
        var operatorId = Guid.NewGuid();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/devices/{device.Id}/assignment",
            new AssignDeviceResource(operatorId, "CAT-797F-012"));
        var devices = await _client.GetFromJsonAsync<List<DeviceResource>>($"/api/v1/devices?operatorId={operatorId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var assigned = Assert.Single(devices!);
        Assert.Equal(device.Id, assigned.Id);
        Assert.Equal(operatorId, assigned.AssignedOperatorId);
        Assert.Equal("CAT-797F-012", assigned.VehicleCode);
    }

    [Fact]
    public async Task PutAssignment_WithoutOperator_ReturnsUnprocessableEntity()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var response = await _client.PutAsJsonAsync($"/api/v1/devices/{device.Id}/assignment",
            new AssignDeviceResource(Guid.Empty, "CAT-01"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostHeartbeat_WithLowBattery_ReturnsDegradedDevice()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/heartbeats",
            new ReportHeartbeatResource(12));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<DeviceResource>();
        Assert.Equal("Degraded", updated!.Status);
        Assert.Equal(12, updated.BatteryLevel);
        Assert.NotNull(updated.LastSeenAt);
        Assert.Equal("LowBattery", Assert.Single(updated.Failures).Kind);
    }

    [Fact]
    public async Task PostHeartbeat_WithBatteryOutOfRange_ReturnsUnprocessableEntity()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/heartbeats",
            new ReportHeartbeatResource(150));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task DisconnectionThenRecovery_RecordsFailureAndRestoresStatus()
    {
        // Arrange
        var device = await RegisterDeviceAsync("VibrationAlarm");

        // Act
        var disconnected = await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/disconnections",
            new ReportDisconnectionResource("Signal lost"));
        var disconnectedDevices = await _client.GetFromJsonAsync<List<DeviceResource>>("/api/v1/devices?status=Disconnected");
        var recovered = await _client.PostAsync($"/api/v1/devices/{device.Id}/recoveries", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, disconnected.StatusCode);
        Assert.Equal("Disconnected", (await disconnected.Content.ReadFromJsonAsync<DeviceResource>())!.Status);
        Assert.Contains(disconnectedDevices!, candidate => candidate.Id == device.Id);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        var recoveredDevice = await recovered.Content.ReadFromJsonAsync<DeviceResource>();
        Assert.Equal("Available", recoveredDevice!.Status);
        var failure = Assert.Single(recoveredDevice.Failures);
        Assert.Equal("Disconnection", failure.Kind);
        Assert.Equal("Signal lost", failure.Detail);
        Assert.NotNull(failure.RecoveredAt);
    }

    [Fact]
    public async Task PostDisconnection_Twice_ReturnsUnprocessableEntity()
    {
        // Arrange
        var device = await RegisterDeviceAsync();
        await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/disconnections", new ReportDisconnectionResource(null));

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/disconnections",
            new ReportDisconnectionResource(null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostRecovery_WithoutDisconnection_ReturnsUnprocessableEntity()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var response = await _client.PostAsync($"/api/v1/devices/{device.Id}/recoveries", null);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetDevices_WithoutFilter_ReturnsAllDevices()
    {
        // Arrange
        var device = await RegisterDeviceAsync();

        // Act
        var devices = await _client.GetFromJsonAsync<List<DeviceResource>>("/api/v1/devices");

        // Assert
        Assert.Contains(devices!, candidate => candidate.Id == device.Id);
    }

    [Fact]
    public async Task GetDevices_ByAvailableStatus_ExcludesOtherStatuses()
    {
        // Arrange
        var degraded = await RegisterDeviceAsync();
        await _client.PostAsJsonAsync($"/api/v1/devices/{degraded.Id}/heartbeats", new ReportHeartbeatResource(5));

        // Act
        var devices = await _client.GetFromJsonAsync<List<DeviceResource>>("/api/v1/devices?status=available");

        // Assert
        Assert.All(devices!, candidate => Assert.Equal("Available", candidate.Status));
        Assert.DoesNotContain(devices!, candidate => candidate.Id == degraded.Id);
    }

    [Theory]
    [InlineData("Broken")]
    [InlineData("1")]
    public async Task GetDevices_WithUnknownStatus_ReturnsUnprocessableEntity(string status)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/devices?status={status}");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Actions_OnUnknownDevice_ReturnNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var assignment = await _client.PutAsJsonAsync($"/api/v1/devices/{unknownId}/assignment",
            new AssignDeviceResource(Guid.NewGuid(), "CAT-01"));
        var heartbeat = await _client.PostAsJsonAsync($"/api/v1/devices/{unknownId}/heartbeats",
            new ReportHeartbeatResource(50));
        var disconnection = await _client.PostAsJsonAsync($"/api/v1/devices/{unknownId}/disconnections",
            new ReportDisconnectionResource(null));
        var recovery = await _client.PostAsync($"/api/v1/devices/{unknownId}/recoveries", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, assignment.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, heartbeat.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, disconnection.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, recovery.StatusCode);
    }
}
