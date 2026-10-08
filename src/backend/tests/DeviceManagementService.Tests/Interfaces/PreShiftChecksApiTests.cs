using System.Net;
using System.Net.Http.Json;
using DeviceManagementService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DeviceManagementService.Tests.Interfaces;

public class PreShiftChecksApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string VehicleCode = "CAT-797F-012";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task AssignDeviceAsync(Guid operatorId, string type, int? batteryLevel = 90)
    {
        var registered = await _client.PostAsJsonAsync("/api/v1/devices",
            new RegisterDeviceResource($"{type}-{Guid.NewGuid():N}", type));
        Assert.True(registered.IsSuccessStatusCode, await registered.Content.ReadAsStringAsync());
        var device = (await registered.Content.ReadFromJsonAsync<DeviceResource>())!;

        await _client.PutAsJsonAsync($"/api/v1/devices/{device.Id}/assignment", new AssignDeviceResource(operatorId, VehicleCode));
        await _client.PostAsJsonAsync($"/api/v1/devices/{device.Id}/heartbeats", new ReportHeartbeatResource(batteryLevel));
    }

    private async Task<PreShiftCheckResource> RunCheckAsync(Guid operatorId)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/pre-shift-checks",
            new RunPreShiftCheckResource(operatorId, VehicleCode));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PreShiftCheckResource>())!;
    }

    [Fact]
    public async Task PostCheck_AllDevicesAvailable_ReturnsFullProtection()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await AssignDeviceAsync(operatorId, "EdgeGateway", null);
        await AssignDeviceAsync(operatorId, "Camera", null);
        await AssignDeviceAsync(operatorId, "VibrationAlarm");
        await AssignDeviceAsync(operatorId, "Wearable");

        // Act
        var check = await RunCheckAsync(operatorId);

        // Assert
        Assert.Equal("Full", check.Protection);
        Assert.True(check.CanStartMonitoring);
        Assert.Equal(4, check.Items.Count);
        Assert.All(check.Items, item => Assert.Equal("Ok", item.Result));
    }

    [Fact]
    public async Task PostCheck_LowBatteryAlarm_ReturnsLimitedWithRecommendedAction()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await AssignDeviceAsync(operatorId, "EdgeGateway", null);
        await AssignDeviceAsync(operatorId, "Camera", null);
        await AssignDeviceAsync(operatorId, "VibrationAlarm", 8);
        await AssignDeviceAsync(operatorId, "Wearable");

        // Act
        var check = await RunCheckAsync(operatorId);

        // Assert
        Assert.Equal("Limited", check.Protection);
        var alarm = Assert.Single(check.Items, item => item.DeviceType == "VibrationAlarm");
        Assert.Equal("Warning", alarm.Result);
        Assert.False(string.IsNullOrWhiteSpace(alarm.RecommendedAction));
    }

    [Fact]
    public async Task PostCheck_WithoutCamera_ReturnsNotAvailable()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await AssignDeviceAsync(operatorId, "EdgeGateway", null);
        await AssignDeviceAsync(operatorId, "VibrationAlarm");

        // Act
        var check = await RunCheckAsync(operatorId);

        // Assert
        Assert.Equal("NotAvailable", check.Protection);
        Assert.False(check.CanStartMonitoring);
        Assert.Equal("Missing", Assert.Single(check.Items, item => item.DeviceType == "Camera").Result);
    }

    [Fact]
    public async Task PostCheck_WithoutOperator_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/pre-shift-checks",
            new RunPreShiftCheckResource(Guid.Empty, VehicleCode));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCheckById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var check = await RunCheckAsync(Guid.NewGuid());

        // Act
        var found = await _client.GetAsync($"/api/v1/pre-shift-checks/{check.Id}");
        var missing = await _client.GetAsync($"/api/v1/pre-shift-checks/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(check.Id, (await found.Content.ReadFromJsonAsync<PreShiftCheckResource>())!.Id);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetLatestCheck_ByOperator_ReturnsOkAndNotFound()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var check = await RunCheckAsync(operatorId);

        // Act
        var found = await _client.GetAsync($"/api/v1/pre-shift-checks/latest?operatorId={operatorId}");
        var missing = await _client.GetAsync($"/api/v1/pre-shift-checks/latest?operatorId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(check.Id, (await found.Content.ReadFromJsonAsync<PreShiftCheckResource>())!.Id);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }
}
