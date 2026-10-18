using System.Net;
using System.Net.Http.Json;
using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Infrastructure.Alerts;

namespace FeedbackReliabilityService.Tests.Infrastructure;

public class HttpAlertSourceTests
{
    [Fact]
    public async Task Find_ValidAlert_MapsExistingRestContract()
    {
        // Arrange
        var id = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var source = Source(new(HttpStatusCode.OK) { Content = JsonContent.Create(new SourceAlert(id, owner, DateTimeOffset.UtcNow)) });

        // Act
        var alert = await source.FindAsync(id, CancellationToken.None);

        // Assert
        Assert.Equal(owner, alert!.OperatorId);
        Assert.Equal(id, alert.Id);
    }

    [Fact]
    public async Task Find_MissingAlert_ReturnsNull()
    {
        // Arrange
        var source = Source(new(HttpStatusCode.NotFound));

        // Act
        var alert = await source.FindAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.Null(alert);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "{}")]
    [InlineData(HttpStatusCode.Unauthorized, "{}")]
    [InlineData(HttpStatusCode.OK, "not-json")]
    [InlineData(HttpStatusCode.OK, "{}")]
    public async Task Find_InvalidUpstream_IsUnavailableNotAnEmptyResult(HttpStatusCode status, string body)
    {
        // Arrange
        var source = Source(new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") });

        // Act
        var action = () => source.FindAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<AlertSourceUnavailableException>(action);
    }

    private static HttpAlertSource Source(HttpResponseMessage response) =>
        new(new HttpClient(new StubHandler(response)) { BaseAddress = new("http://alerts.invalid/") });

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response);
    }
}
