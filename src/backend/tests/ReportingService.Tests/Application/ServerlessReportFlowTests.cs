using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Domain.Model;
using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Application.Internal.Serverless;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Infrastructure.Rendering;
using LambdaKey = ReportPdfGenerator.Storage.ReportObjectKey;
using LambdaRequest = ReportPdfGenerator.Model.ReportRequest;
using LambdaValidator = ReportPdfGenerator.Model.ReportRequestValidator;

namespace ReportingService.Tests.Application;

public class ServerlessReportFlowTests
{
    private static readonly DateTimeOffset RequestedAt = new(2026, 10, 31, 23, 50, 0, TimeSpan.FromHours(-5));
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(40);
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private readonly FakeReportDefinitionRepository definitions = new();
    private readonly FakeReportJobRepository jobs = new();
    private readonly FakeReportArtifactRepository artifacts = new();
    private readonly FakeReportRequestQueue queue = new();
    private readonly FakeReportObjectStore store = new();
    private readonly ManualTimeProvider clock = new(RequestedAt);

    private async Task<ReportingCommandService> Commands(FakeReportRequestQueue? requestQueue)
    {
        await definitions.AddAsync(new ReportDefinition(new DefineReportIndicatorCommand(
            "fatigue-by-shift", "Fatigue alerts by shift", "v1.0", ["alerts", "incidents"])));
        return new ReportingCommandService(definitions, jobs, artifacts, new InProcessReportRenderer(), clock, requestQueue);
    }

    private ReportingQueryService Queries() => new(definitions, jobs, artifacts, store);

    private ReportCompletionService Completion() => new(jobs, artifacts, store,
        new ServerlessReportingOptions("https://sqs.test/queue", "bucket", Timeout, TimeSpan.FromSeconds(15)),
        clock, NullLogger<ReportCompletionService>.Instance);

    private static RequestReportGenerationCommand Command(ReportFormat format = ReportFormat.Pdf, string? siteId = "site-north") =>
        new(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            siteId, "fleet-a", "fatigue-by-shift", format, Guid.NewGuid());

    [Fact]
    public async Task RequestPdf_Serverless_PublishesAndStaysRenderingWithoutArtifact()
    {
        // Arrange
        var commands = await Commands(queue);

        // Act
        var job = await commands.Handle(Command());

        // Assert
        Assert.Equal(ReportJobStatus.Rendering, job.Status);
        Assert.Null(job.CompletedAt);
        Assert.Single(queue.Messages);
        Assert.Null(await artifacts.FindByReportJobIdAsync(job.Id));
    }

    [Fact]
    public async Task RequestPdf_Serverless_MessagePassesTheLambdaContract()
    {
        // Arrange
        var commands = await Commands(queue);
        var command = Command();

        // Act
        var job = await commands.Handle(command);
        var request = JsonSerializer.Deserialize<LambdaRequest>(Assert.Single(queue.Messages), Web);

        // Assert
        Assert.Empty(LambdaValidator.Validate(request));
        Assert.Equal(job.Id, request!.ReportId);
        Assert.Equal("Fatigue alerts by shift (v1.0)", request.Title);
        Assert.Equal(command.From, request.PeriodFrom);
        Assert.Equal(command.To, request.PeriodTo);
        Assert.Equal(command.RequestedBy.ToString(), request.RequestedBy);
        Assert.Contains("site-north", request.Scope);
        Assert.Contains("fleet-a", request.Scope);
        Assert.Equal(job.RequestedAt, request.RequestedAt);
        Assert.Contains(request.Sections.Single().Rows!, row => row.SequenceEqual(["Fuentes de datos", "alerts incidents"]));
    }

    [Fact]
    public async Task RequestPdf_Serverless_SendsNoInventedFigures()
    {
        // Arrange
        var commands = await Commands(queue);

        // Act
        await commands.Handle(Command());
        var summary = JsonSerializer.Deserialize<LambdaRequest>(queue.Messages.Single(), Web)!.Summary;

        // Assert
        Assert.Equal(new ReportPdfGenerator.Model.ReportSummary(null, null, null, null, null), summary);
    }

    [Fact]
    public void ObjectKey_IsTheOneTheLambdaUses()
    {
        // Arrange: in UTC this moment is already in November
        var reportId = Guid.NewGuid();

        // Act
        var key = ReportObjectKey.For(reportId, RequestedAt);

        // Assert
        Assert.Equal(LambdaKey.For(reportId, RequestedAt), key);
        Assert.Equal($"reports/2026/11/{reportId}.pdf", key);
    }

    [Fact]
    public async Task RequestPdf_Serverless_QueueDown_FailsTheJobWithoutLeakingTheError()
    {
        // Arrange
        var commands = await Commands(queue);
        queue.Failure = new InvalidOperationException("arn:aws:iam::123:role/secret denied");

        // Act
        var job = await commands.Handle(Command());

        // Assert
        Assert.Equal(ReportJobStatus.Failed, job.Status);
        Assert.Equal("The report could not be queued for generation.", job.FailureReason);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public async Task RequestPdf_Serverless_EmptyPeriodTheLambdaWouldDiscard_IsRejected()
    {
        // Arrange
        var commands = await Commands(queue);
        var command = Command() with { To = Command().From };

        // Act + Assert
        await Assert.ThrowsAsync<DomainException>(() => commands.Handle(command));
        Assert.Empty(queue.Messages);
        Assert.Empty(await jobs.ListAsync());
    }

    [Fact]
    public async Task RequestCsv_Serverless_StaysInProcess()
    {
        // Arrange
        var commands = await Commands(queue);

        // Act
        var job = await commands.Handle(Command(ReportFormat.Csv));

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        Assert.Empty(queue.Messages);
        var artifact = await artifacts.FindByReportJobIdAsync(job.Id);
        Assert.Null(artifact!.StorageKey);
        Assert.False(artifact.Content.IsEmpty);
    }

    [Fact]
    public async Task RequestPdf_NotConfigured_RendersInProcess()
    {
        // Arrange
        var commands = await Commands(requestQueue: null);

        // Act
        var job = await commands.Handle(Command());

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        var artifact = await artifacts.FindByReportJobIdAsync(job.Id);
        Assert.Null(artifact!.StorageKey);
        Assert.Equal("%PDF"u8.ToArray(), artifact.Content.ToArray()[..4]);
    }

    [Fact]
    public async Task Check_ObjectExists_CompletesWithStoredArtifact()
    {
        // Arrange
        var job = await (await Commands(queue)).Handle(Command());
        var key = ReportObjectKey.For(job.Id, job.RequestedAt);
        byte[] pdf = [1, 2, 3];
        store.Objects[key] = pdf;
        clock.Advance(TimeSpan.FromMinutes(1));

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        Assert.Equal(clock.GetUtcNow(), job.CompletedAt);
        var artifact = await artifacts.FindByReportJobIdAsync(job.Id);
        Assert.Equal(key, artifact!.StorageKey);
        Assert.Equal($"/api/v1/reports/{job.Id}/download", artifact.Uri);
        Assert.Equal("application/pdf", artifact.ContentType);
        Assert.Equal($"fatigue-by-shift-{job.Id:N}.pdf", artifact.FileName);
        Assert.Equal($"sha256:{Convert.ToHexString(SHA256.HashData(pdf)).ToLowerInvariant()}", artifact.Checksum);
        Assert.True(artifact.Content.IsEmpty);
    }

    [Fact]
    public async Task Check_ObjectMissingWithinTimeout_KeepsRendering()
    {
        // Arrange
        var job = await (await Commands(queue)).Handle(Command());
        clock.Advance(Timeout - TimeSpan.FromSeconds(1));

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Rendering, job.Status);
    }

    [Fact]
    public async Task Check_ObjectMissingAfterTimeout_FailsTheJob()
    {
        // Arrange
        var job = await (await Commands(queue)).Handle(Command());
        clock.Advance(Timeout);

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Failed, job.Status);
        Assert.Contains("40 minutes", job.FailureReason);
    }

    [Fact]
    public async Task Check_StorageUnavailable_StillTimesOut()
    {
        // Arrange
        var commands = await Commands(queue);
        var job = await commands.Handle(Command());
        store.Failure = new IOException("S3 unavailable");

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);
        var before = job.Status;
        clock.Advance(Timeout);
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Rendering, before);
        Assert.Equal(ReportJobStatus.Failed, job.Status);
    }

    [Fact]
    public async Task Check_ObjectExistsAfterTimeout_WinsOverTimeout()
    {
        // Arrange
        var job = await (await Commands(queue)).Handle(Command());
        store.Objects[ReportObjectKey.For(job.Id, job.RequestedAt)] = [9];
        clock.Advance(Timeout + TimeSpan.FromMinutes(5));

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
    }

    [Fact]
    public async Task Check_ArtifactAlreadyStored_OnlyFinishesTheJob()
    {
        // Arrange: another replica stopped between storing the artifact and updating the job
        var job = await (await Commands(queue)).Handle(Command());
        var artifact = ReportArtifact.Stored(job.Id, "/download", "report.pdf", "application/pdf", "key", [1], clock.GetUtcNow());
        await artifacts.AddAsync(artifact);

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        Assert.Equal(artifact.Id, (await artifacts.FindByReportJobIdAsync(job.Id))!.Id);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task Check_OtherReplicaStoresFirst_ConflictIsNotAFailure()
    {
        // Arrange
        var conflicting = new ConflictingArtifactRepository();
        var job = await (await Commands(queue)).Handle(Command());
        store.Objects[ReportObjectKey.For(job.Id, job.RequestedAt)] = [1];
        var completion = new ReportCompletionService(jobs, conflicting, store,
            new ServerlessReportingOptions("q", "b", Timeout, TimeSpan.FromSeconds(15)),
            clock, NullLogger<ReportCompletionService>.Instance);

        // Act
        await completion.CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
    }

    [Fact]
    public async Task Check_RenderingCsv_IsLeftAlone()
    {
        // Arrange: a CSV being rendered by its own request when the poller runs
        await Commands(queue);
        var job = new ReportJob(Command(ReportFormat.Csv), "v1.0", clock.GetUtcNow());
        job.StartRendering();
        await jobs.AddAsync(job);
        clock.Advance(Timeout);

        // Act
        await Completion().CheckPendingJobsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(ReportJobStatus.Rendering, job.Status);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task OpenStoredArtifact_ReturnsTheObject_OrNullWhenItIsGone()
    {
        // Arrange
        var artifact = ReportArtifact.Stored(Guid.NewGuid(), "/download", "r.pdf", "application/pdf", "key", [1], clock.GetUtcNow());
        var inProcess = new ReportArtifact(Guid.NewGuid(), "/download", "r.csv", "text/csv", [1], clock.GetUtcNow());

        // Act + Assert
        Assert.Null(await Queries().OpenStoredArtifactAsync(artifact, CancellationToken.None));
        store.Objects["key"] = [7, 8];
        await using var content = await Queries().OpenStoredArtifactAsync(artifact, CancellationToken.None);
        using var copy = new MemoryStream();
        await content!.CopyToAsync(copy);
        Assert.Equal([7, 8], copy.ToArray());
        Assert.Null(await Queries().OpenStoredArtifactAsync(inProcess, CancellationToken.None));
    }

    [Fact]
    public void Options_NeitherSetting_MeansInProcess()
    {
        Assert.Null(ServerlessReportingOptions.FromConfiguration(Configuration(("Reporting:SqsQueueUrl", " "))));
    }

    [Theory]
    [InlineData("Reporting:SqsQueueUrl")]
    [InlineData("Reporting:S3Bucket")]
    public void Options_OnlyOneSetting_StopsTheStart(string key)
    {
        Assert.Throws<InvalidOperationException>(() =>
            ServerlessReportingOptions.FromConfiguration(Configuration((key, "value"))));
    }

    [Fact]
    public void Options_BothSettings_UseDefaultsAndOverrides()
    {
        // Act
        var defaults = ServerlessReportingOptions.FromConfiguration(Configuration(
            ("Reporting:SqsQueueUrl", "url"), ("Reporting:S3Bucket", "bucket")));
        var custom = ServerlessReportingOptions.FromConfiguration(Configuration(
            ("Reporting:SqsQueueUrl", "url"), ("Reporting:S3Bucket", "bucket"),
            ("Reporting:GenerationTimeoutMinutes", "5"), ("Reporting:PollIntervalSeconds", "2")));

        // Assert
        Assert.Equal(new ServerlessReportingOptions("url", "bucket", TimeSpan.FromMinutes(40), TimeSpan.FromSeconds(15)), defaults);
        Assert.Equal(new ServerlessReportingOptions("url", "bucket", TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2)), custom);
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private sealed class ConflictingArtifactRepository : FakeReportArtifactRepository
    {
        public override Task AddAsync(ReportArtifact aggregate) => throw new DomainConflictException("duplicate");
    }
}
