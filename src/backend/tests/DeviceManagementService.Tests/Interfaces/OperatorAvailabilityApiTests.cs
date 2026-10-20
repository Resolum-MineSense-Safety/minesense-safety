using System.Net;
using System.Net.Http.Json;
using DeviceManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DeviceManagementService.Tests.Interfaces;

public class OperatorAvailabilityApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetOperatorAvailability_WithAvailableDevice_ReportsActiveMonitoring()
    {
        var operatorId = Guid.NewGuid();
        var device = await RegisterAsync("operator-active");
        await AssignAsync(device.Id, operatorId);
        await ReportStatusAsync(device.Id, "Available");

        var result = await GetOperatorAsync(operatorId);

        Assert.Equal("Active", result.Status);
        Assert.Equal(device.Id, Assert.Single(result.Devices).Id);
    }

    [Fact]
    public async Task GetOperatorAvailability_AfterDisconnectionAndRecovery_UpdatesStatus()
    {
        var operatorId = Guid.NewGuid();
        var device = await RegisterAsync("operator-recovery");
        await AssignAsync(device.Id, operatorId);

        await ReportStatusAsync(device.Id, "Interrupted");
        Assert.Equal("Unavailable", (await GetOperatorAsync(operatorId)).Status);

        await ReportStatusAsync(device.Id, "Available");
        Assert.Equal("Active", (await GetOperatorAsync(operatorId)).Status);
    }

    private async Task<OperatorAvailabilityResource> GetOperatorAsync(Guid operatorId)
    {
        var response = await _client.GetAsync("/api/v1/devices/operator-availability");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<OperatorAvailabilityResource>>();
        return result!.Single(availability => availability.OperatorId == operatorId);
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

    private async Task ReportStatusAsync(Guid deviceId, string status)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/v1/devices/{deviceId}/status",
            new ReportDeviceStatusResource(status, 80, DateTimeOffset.UtcNow, []));
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }
}
