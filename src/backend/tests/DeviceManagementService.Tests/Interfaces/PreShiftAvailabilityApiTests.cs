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
    public async Task GetPreShiftAvailability_WithAvailableCamera_ReturnsAvailable()
    {
        var operatorId = Guid.NewGuid();
        var device = await RegisterAsync("pre-shift-available");
        await AssignAsync(device.Id, operatorId);
        await ReportStatusAsync(device.Id, "Available", 80);

        var response = await _client.GetAsync(
            $"/api/v1/devices/pre-shift-availability?operatorId={operatorId}&batteryThreshold=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResource>();
        Assert.Equal("Available", result!.Status);
        Assert.Equal("StartMonitoring", result.Recommendation);
    }

    [Fact]
    public async Task GetPreShiftAvailability_WithLowBattery_ReturnsWarning()
    {
        var operatorId = Guid.NewGuid();
        var device = await RegisterAsync("pre-shift-warning");
        await AssignAsync(device.Id, operatorId);
        await ReportStatusAsync(device.Id, "Available", 20);

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

    private async Task<DeviceResource> RegisterAsync(string code)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource(code, "camera", Guid.NewGuid()));
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
