using System.Net;
using System.Text;
using MonitoringService.Infrastructure.Operations;

namespace MonitoringService.Tests.Infrastructure;

public class HttpOperationalContextServiceTests
{
    private static readonly DateOnly ShiftDate = new(2026, 10, 6);

    private static (HttpOperationalContextService Service, StubHttpMessageHandler Handler) CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHttpMessageHandler(respond);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://operations.test/") };
        return (new HttpOperationalContextService(client), handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
        new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task ValidateAsync_ValidContext_ReturnsAssignmentAndCallsExpectedUri()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var assignmentId = Guid.NewGuid();
        var (service, handler) = CreateService(_ =>
            Json($$"""{"valid":true,"assignmentId":"{{assignmentId}}","reason":null}"""));

        // Act
        var context = await service.ValidateAsync(operatorId, "CAM 001", ShiftDate);

        // Assert
        Assert.True(context.Valid);
        Assert.Equal(assignmentId, context.AssignmentId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.Equal(
            $"http://operations.test/api/v1/operator-assignments/context?operatorId={operatorId}&vehicleCode=CAM%20001&date=2026-10-06",
            handler.LastRequest.RequestUri!.AbsoluteUri);
    }

    [Fact]
    public async Task ValidateAsync_InvalidContext_ReturnsReason()
    {
        // Arrange
        var (service, _) = CreateService(_ =>
            Json("""{"valid":false,"assignmentId":null,"reason":"No assignment for the shift."}"""));

        // Act
        var context = await service.ValidateAsync(Guid.NewGuid(), "CAM-001", ShiftDate);

        // Assert
        Assert.False(context.Valid);
        Assert.Null(context.AssignmentId);
        Assert.Equal("No assignment for the shift.", context.Reason);
    }

    [Fact]
    public async Task ValidateAsync_ErrorStatusCode_ReturnsInvalid()
    {
        // Arrange
        var (service, _) = CreateService(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        // Act
        var context = await service.ValidateAsync(Guid.NewGuid(), "CAM-001", ShiftDate);

        // Assert
        Assert.False(context.Valid);
        Assert.Contains("500", context.Reason);
    }

    [Fact]
    public async Task ValidateAsync_EmptyBody_ReturnsInvalid()
    {
        // Arrange
        var (service, _) = CreateService(_ => Json("null"));

        // Act
        var context = await service.ValidateAsync(Guid.NewGuid(), "CAM-001", ShiftDate);

        // Assert
        Assert.False(context.Valid);
    }

    [Fact]
    public async Task ValidateAsync_MalformedBody_ReturnsInvalid()
    {
        // Arrange
        var (service, _) = CreateService(_ => Json("not-json"));

        // Act
        var context = await service.ValidateAsync(Guid.NewGuid(), "CAM-001", ShiftDate);

        // Assert
        Assert.False(context.Valid);
    }

    [Fact]
    public async Task ValidateAsync_ServiceUnreachable_ReturnsInvalid()
    {
        // Arrange
        var (service, _) = CreateService(_ => throw new HttpRequestException("Connection refused"));

        // Act
        var context = await service.ValidateAsync(Guid.NewGuid(), "CAM-001", ShiftDate);

        // Assert
        Assert.False(context.Valid);
        Assert.Contains("Connection refused", context.Reason);
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
