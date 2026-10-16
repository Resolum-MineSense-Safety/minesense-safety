using System.Net;
using System.Net.Http.Json;
using DeviceManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DeviceManagementService.Tests.Interfaces;

public class DevicesApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostDevice_WithValidResource_ReturnsCreatedDevice()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource("cam-api-01", "camera", Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var device = await response.Content.ReadFromJsonAsync<DeviceResource>();
        Assert.Equal("cam-api-01", device!.DeviceCode);
        Assert.Null(device.Status);
    }

    [Fact]
    public async Task PostDevice_WithDuplicateCode_ReturnsConflict()
    {
        var resource = new RegisterDeviceResource("cam-api-duplicate", "camera", Guid.NewGuid());
        await _client.PostAsJsonAsync("/api/v1/devices", resource);

        var response = await _client.PostAsJsonAsync("/api/v1/devices", resource);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PutAssignment_AndPostStatus_ReturnExpectedResponses()
    {
        var created = await RegisterAsync("cam-api-lifecycle");
        var operatorId = Guid.NewGuid();

        var assignment = await _client.PutAsJsonAsync(
            $"/api/v1/devices/{created.Id}/assignment",
            new AssignDeviceResource(operatorId, null));
        var status = await _client.PostAsJsonAsync(
            $"/api/v1/devices/{created.Id}/status",
            new ReportDeviceStatusResource(
                "Available", 90, DateTimeOffset.UtcNow, ["camera"]));

        Assert.Equal(HttpStatusCode.OK, assignment.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, status.StatusCode);
        var reported = await status.Content.ReadFromJsonAsync<DeviceResource>();
        Assert.Equal("Available", reported!.Status);
        Assert.Equal(90, reported.BatteryLevel);
    }

    [Fact]
    public async Task PostStatus_ForUnknownDevice_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/devices/{Guid.NewGuid()}/status",
            new ReportDeviceStatusResource(
                "Interrupted", 0, DateTimeOffset.UtcNow, []));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostStatus_WithUnknownStatus_ReturnsUnprocessableEntity()
    {
        var created = await RegisterAsync("cam-api-invalid-status");

        var response = await _client.PostAsJsonAsync(
            $"/api/v1/devices/{created.Id}/status",
            new ReportDeviceStatusResource(
                "Unknown", 50, DateTimeOffset.UtcNow, []));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetDevices_AndStatus_ReturnExpectedResources()
    {
        var created = await RegisterAsync("cam-api-query");

        var devices = await _client.GetFromJsonAsync<List<DeviceResource>>("/api/v1/devices");
        var status = await _client.GetAsync($"/api/v1/devices/{created.Id}/status");
        var missing = await _client.GetAsync($"/api/v1/devices/{Guid.NewGuid()}/status");

        Assert.Contains(devices!, device => device.Id == created.Id);
        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    private async Task<DeviceResource> RegisterAsync(string code)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource(code, "camera", Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DeviceResource>())!;
    }
}
