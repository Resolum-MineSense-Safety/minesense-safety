using System.Net;
using System.Text;
using MonitoringService.Infrastructure.DeviceManagement;

namespace MonitoringService.Tests.Infrastructure;

public class HttpDeviceAvailabilityServiceTests
{
    private static (HttpDeviceAvailabilityService Service, StubHttpMessageHandler Handler) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHttpMessageHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://devices.test/") };
        return (new HttpDeviceAvailabilityService(client, 20.5m), handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task CheckAsync_AvailableDevices_AllowsMonitoringAndCallsExpectedUri()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var (service, handler) = CreateService(_ =>
            Json("""{"status":"Available","recommendation":"StartMonitoring","evaluatedDevices":[]}"""));

        // Act
        var availability = await service.CheckAsync(operatorId);

        // Assert
        Assert.True(availability.CanStartMonitoring);
        Assert.Null(availability.Reason);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            $"http://devices.test/api/v1/devices/pre-shift-availability?operatorId={operatorId}&batteryThreshold=20.5",
            handler.LastRequest.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task CheckAsync_LowBatteryWarning_AllowsMonitoring()
    {
        // Arrange
        var (service, _) = CreateService(_ =>
            Json("""{"status":"Warning","recommendation":"ReviewBatteryBeforeStarting","evaluatedDevices":[]}"""));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.True(availability.CanStartMonitoring);
    }

    [Fact]
    public async Task CheckAsync_IncompleteProtection_ReturnsUnavailableWithStatus()
    {
        // Arrange
        var (service, _) = CreateService(_ =>
            Json("""{"status":"ProtectionIncomplete","recommendation":"DoNotStartMonitoring","evaluatedDevices":[]}"""));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.False(availability.CanStartMonitoring);
        Assert.Contains("ProtectionIncomplete", availability.Reason);
    }

    [Fact]
    public async Task CheckAsync_ErrorStatusCode_ReturnsUnavailable()
    {
        // Arrange
        var (service, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.False(availability.CanStartMonitoring);
        Assert.Contains("500", availability.Reason);
    }

    [Fact]
    public async Task CheckAsync_EmptyBody_ReturnsUnavailable()
    {
        // Arrange
        var (service, _) = CreateService(_ => Json("null"));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.False(availability.CanStartMonitoring);
    }

    [Fact]
    public async Task CheckAsync_MalformedBody_ReturnsUnavailable()
    {
        // Arrange
        var (service, _) = CreateService(_ => Json("not-json"));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.False(availability.CanStartMonitoring);
    }

    [Fact]
    public async Task CheckAsync_ServiceUnreachable_ReturnsUnavailable()
    {
        // Arrange
        var (service, _) = CreateService(_ => throw new HttpRequestException("Connection refused"));

        // Act
        var availability = await service.CheckAsync(Guid.NewGuid());

        // Assert
        Assert.False(availability.CanStartMonitoring);
        Assert.Contains("Connection refused", availability.Reason);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(respond(request));
        }
    }
}
