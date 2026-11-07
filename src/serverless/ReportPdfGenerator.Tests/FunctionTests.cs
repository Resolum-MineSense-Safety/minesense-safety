using System.Text.Json;
using Amazon.Lambda.SQSEvents;
using Amazon.Lambda.TestUtilities;
using ReportPdfGenerator.Storage;

namespace ReportPdfGenerator.Tests;

public class FunctionTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private readonly FakeStorage _storage = new();
    private readonly Function _function;

    public FunctionTests() =>
        _function = new Function(_storage, new FixedTimeProvider(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero)));

    private static SQSEvent.SQSMessage Message(string id, string body) => new() { MessageId = id, Body = body };

    [Fact]
    public async Task FunctionHandler_ValidRequest_StoresPdfAndReportsNoFailures()
    {
        // Arrange
        var request = ReportRequests.Valid();
        var sqsEvent = new SQSEvent { Records = [Message("m1", JsonSerializer.Serialize(request, Web))] };

        // Act
        var response = await _function.FunctionHandler(sqsEvent, new TestLambdaContext());

        // Assert
        Assert.Empty(response.BatchItemFailures);
        var (reportId, document) = Assert.Single(_storage.Saved);
        Assert.Equal(request.ReportId, reportId);
        Assert.Equal("%PDF"u8.ToArray(), document[..4]);
    }

    [Fact]
    public async Task FunctionHandler_InvalidRequest_IsDiscardedWithoutRetry()
    {
        // Arrange
        var sqsEvent = new SQSEvent { Records = [Message("bad-json", "{ not json"), Message("empty", "{}")] };

        // Act
        var response = await _function.FunctionHandler(sqsEvent, new TestLambdaContext());

        // Assert
        Assert.Empty(response.BatchItemFailures);
        Assert.Empty(_storage.Saved);
    }

    [Fact]
    public async Task FunctionHandler_StorageFailure_ReturnsOnlyThatMessageForRetry()
    {
        // Arrange
        _storage.FailFor = ReportRequests.Valid();
        var ok = ReportRequests.Valid();
        var sqsEvent = new SQSEvent
        {
            Records =
            [
                Message("fails", JsonSerializer.Serialize(_storage.FailFor, Web)),
                Message("works", JsonSerializer.Serialize(ok, Web))
            ]
        };

        // Act
        var response = await _function.FunctionHandler(sqsEvent, new TestLambdaContext());

        // Assert
        Assert.Equal("fails", Assert.Single(response.BatchItemFailures).ItemIdentifier);
        Assert.Equal(ok.ReportId, Assert.Single(_storage.Saved).ReportId);
    }

    [Fact]
    public async Task ProcessAsync_ReturnsStorageKey()
    {
        // Arrange
        var request = ReportRequests.Valid();

        // Act
        var key = await _function.ProcessAsync(JsonSerializer.Serialize(request, Web), CancellationToken.None);

        // Assert
        Assert.Equal($"fake/{request.ReportId}.pdf", key);
    }

    [Fact]
    public async Task ProcessAsync_SampleRequestFile_IsValid()
    {
        // Arrange
        var body = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "sample-request.json"));

        // Act
        var key = await _function.ProcessAsync(body, CancellationToken.None);

        // Assert
        Assert.EndsWith(".pdf", key);
    }

    private sealed class FakeStorage : IReportStorage
    {
        public List<(Guid ReportId, byte[] Document)> Saved { get; } = [];
        public Model.ReportRequest? FailFor { get; set; }

        public Task<string> SaveAsync(Guid reportId, byte[] document, CancellationToken cancellationToken)
        {
            if (FailFor?.ReportId == reportId) throw new IOException("S3 unavailable");
            Saved.Add((reportId, document));
            return Task.FromResult($"fake/{reportId}.pdf");
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
