using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReportingService.Application.Internal.Serverless;
using ReportingService.Domain.Services;
using ReportingService.Tests.Application;

namespace ReportingService.Tests.Interfaces;

/// <summary>The whole service with the flow configured; only SQS and S3 are replaced by fakes.</summary>
public class ServerlessReportsApiTests : IDisposable
{
    private readonly FakeReportRequestQueue queue = new();
    private readonly FakeReportObjectStore store = new();
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public ServerlessReportsApiTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Reporting:SqsQueueUrl", "https://sqs.invalid/queue");
            builder.UseSetting("Reporting:S3Bucket", "reports-bucket");
            builder.UseSetting("Reporting:PollIntervalSeconds", "1");
            builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IReportRequestQueue>(queue));
                services.Replace(ServiceDescriptor.Singleton<IReportObjectStore>(store));
            });
        });
        client = factory.CreateClient();
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }

    private async Task<JsonElement> RequestReport(string format = "Pdf")
    {
        await client.PostAsJsonAsync("/api/v1/report-definitions", new
        {
            code = "serverless-e2e",
            name = "Serverless e2e",
            version = "v1.0",
            dataSources = new[] { "alerts" }
        });
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-14T00:00:00Z",
            siteId = "site-north",
            definitionCode = "serverless-e2e",
            format,
            requestedBy = Guid.NewGuid()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<string?> WaitForStatus(string id, string status)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var job = await client.GetFromJsonAsync<JsonElement>($"/api/v1/reports/{id}");
            if (job.GetProperty("status").GetString() == status) return status;
            await Task.Delay(100);
        }
        return null;
    }

    [Fact]
    public async Task PdfReport_GoesThroughQueueAndStorageUntilDownloaded()
    {
        // Act: requested
        var job = await RequestReport();
        var id = job.GetProperty("id").GetString()!;

        // Assert: delegated, nothing to download yet
        Assert.Equal("Rendering", job.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, job.GetProperty("downloadUri").ValueKind);
        Assert.Single(queue.Messages);
        Assert.Equal(HttpStatusCode.Conflict, (await client.GetAsync($"/api/v1/reports/{id}/download")).StatusCode);

        // Act: the Lambda stores the PDF and the poller notices
        var message = JsonDocument.Parse(queue.Messages[0]).RootElement;
        var key = ReportObjectKey.For(Guid.Parse(id), message.GetProperty("requestedAt").GetDateTimeOffset());
        store.Objects[key] = "%PDF-from-lambda"u8.ToArray();

        // Assert: ready and served from storage
        Assert.Equal("Ready", await WaitForStatus(id, "Ready"));
        var download = await client.GetAsync($"/api/v1/reports/{id}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("application/pdf", download.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith("sha256:", download.Headers.GetValues("X-Checksum").Single());
        Assert.Equal("%PDF-from-lambda"u8.ToArray(), await download.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task PdfReport_ObjectRemovedFromStorage_ReturnsGone()
    {
        // Arrange
        var job = await RequestReport();
        var id = job.GetProperty("id").GetString()!;
        var requestedAt = JsonDocument.Parse(queue.Messages[0]).RootElement.GetProperty("requestedAt").GetDateTimeOffset();
        var key = ReportObjectKey.For(Guid.Parse(id), requestedAt);
        store.Objects[key] = [1];
        await WaitForStatus(id, "Ready");
        store.Objects.TryRemove(key, out _);

        // Act
        var download = await client.GetAsync($"/api/v1/reports/{id}/download");

        // Assert
        Assert.Equal(HttpStatusCode.Gone, download.StatusCode);
    }

    [Fact]
    public async Task CsvReport_IsStillRenderedInProcess()
    {
        // Act
        var job = await RequestReport("Csv");

        // Assert
        Assert.Equal("Ready", job.GetProperty("status").GetString());
        Assert.Empty(queue.Messages);
        var download = await client.GetAsync($"/api/v1/reports/{job.GetProperty("id").GetString()}/download");
        Assert.Equal("text/csv", download.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task PdfReport_QueueDown_ReturnsFailedJob()
    {
        // Arrange
        queue.Failure = new InvalidOperationException("SQS unavailable");

        // Act
        var job = await RequestReport();

        // Assert
        Assert.Equal("Failed", job.GetProperty("status").GetString());
        Assert.Equal("The report could not be queued for generation.", job.GetProperty("failureReason").GetString());
    }
}
