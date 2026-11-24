using System.Net;
using System.Net.Http.Json;
using EdgeSynchronizationService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EdgeSynchronizationService.Tests.Interfaces;

public sealed class SynchronizationBatchesApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/api/v1/synchronization-batches";
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task SubmitBatch_ReturnsAcceptedAndCanBeQueried()
    {
        var resource = CreateResource();

        var response = await _client.PostAsJsonAsync(Route, resource);
        var stored = await response.Content.ReadFromJsonAsync<SynchronizationBatchResource>();

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.NotNull(stored);
        Assert.Equal(resource.BatchId, stored!.BatchId);
        Assert.Equal("Accepted", stored.Status);

        var query = await _client.GetAsync($"{Route}/{resource.BatchId}");
        Assert.Equal(HttpStatusCode.OK, query.StatusCode);
    }

    [Fact]
    public async Task SubmitBatch_RetryWithSameBatchId_IsIdempotent()
    {
        var resource = CreateResource();

        var first = await _client.PostAsJsonAsync(Route, resource);
        var second = await _client.PostAsJsonAsync(Route, resource with
        {
            SequenceNumber = resource.SequenceNumber + 1
        });

        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var stored = await second.Content.ReadFromJsonAsync<SynchronizationBatchResource>();
        Assert.Equal(resource.SequenceNumber, stored!.SequenceNumber);
    }

    [Fact]
    public async Task SubmitBatch_WithoutEvents_ReturnsUnprocessableEntity()
    {
        var response = await _client.PostAsJsonAsync(Route, CreateResource() with
        {
            Events = []
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetBatch_UnknownBatch_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static SubmitSynchronizationBatchResource CreateResource() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 1, DateTimeOffset.UtcNow,
        [
            new(Guid.NewGuid(), DateTimeOffset.UtcNow, "fatigue-assessment",
                System.Text.Json.JsonDocument.Parse("""{"riskLevel":"Warning"}""").RootElement)
        ]);
}
