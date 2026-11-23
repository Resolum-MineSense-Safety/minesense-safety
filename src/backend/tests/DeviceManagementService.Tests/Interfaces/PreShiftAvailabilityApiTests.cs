using System.Net;
using System.Net.Http.Json;
using DeviceManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DeviceManagementService.Tests.Interfaces;

public class PreShiftAvailabilityApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetPreShiftAvailability_WithAvailableCameraAndWearable_ReturnsAvailable()
    {
        var operatorId = Guid.NewGuid();
        await AddAssignedDeviceAsync("pre-shift-available-camera", "camera", operatorId, 80);
        await AddAssignedDeviceAsync("pre-shift-available-wearable", "wearable", operatorId, 80);

        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={operatorId}&batteryThreshold=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResource>();
        Assert.Equal("Available", result!.Status);
        Assert.Equal("StartMonitoring", result.Recommendation);
        Assert.Empty(result.MissingDeviceTypes);
    }

    [Fact]
    public async Task GetPreShiftAvailability_WithoutWearable_ReturnsIncompleteProtection()
    {
        var operatorId = Guid.NewGuid();
        await AddAssignedDeviceAsync("pre-shift-camera-only", "camera", operatorId, 80);

        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={operatorId}&batteryThreshold=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResource>();
        Assert.Equal("ProtectionIncomplete", result!.Status);
        Assert.Equal("DoNotStartMonitoring", result.Recommendation);
        Assert.Equal(["wearable"], result.MissingDeviceTypes);
    }

    [Fact]
    public async Task GetPreShiftAvailability_WithLowBattery_ReturnsWarning()
    {
        var operatorId = Guid.NewGuid();
        await AddAssignedDeviceAsync("pre-shift-warning-camera", "camera", operatorId, 20);
        await AddAssignedDeviceAsync("pre-shift-warning-wearable", "wearable", operatorId, 80);

        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={operatorId}&batteryThreshold=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResource>();
        Assert.Equal("Warning", result!.Status);
        Assert.Equal("ReviewBatteryBeforeStarting", result.Recommendation);
    }

    [Fact]
    public async Task GetPreShiftAvailability_WithoutCamera_ReturnsIncompleteProtection()
    {
        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={Guid.NewGuid()}&batteryThreshold=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResource>();
        Assert.Equal("ProtectionIncomplete", result!.Status);
        Assert.Equal("DoNotStartMonitoring", result.Recommendation);
    }

    [Fact]
    public async Task GetPreShiftAvailability_WithInvalidThreshold_ReturnsUnprocessableEntity()
    {
        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={Guid.NewGuid()}&batteryThreshold=101");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private async Task AddAssignedDeviceAsync(
        string code, string deviceType, Guid operatorId, decimal battery)
    {
        var device = await RegisterAsync(code, deviceType);
        await AssignAsync(device.Id, operatorId);
        await ReportStatusAsync(device.Id, "Available", battery);
    }

    private async Task<DeviceResource> RegisterAsync(string code, string deviceType)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource(code, deviceType, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DeviceResource>())!;
    }

    private async Task AssignAsync(Guid deviceId, Guid operatorId)
    {
        var response = await _client.PutAsJsonAsync(
            $"/api/v1/devices/{deviceId}/assignment",
            new AssignDeviceResource(operatorId, null));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private async Task ReportStatusAsync(Guid deviceId, string status, decimal battery)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/devices/{deviceId}/status",
            new ReportDeviceStatusResource(status, battery, DateTimeOffset.UtcNow, []));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
