using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Domain.Services;
using ReportingService.Infrastructure.Rendering;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Tests.Application;

public class ReportJobServicesTests
{
    private readonly FakeReportDefinitionRepository definitions = new();
    private readonly FakeReportJobRepository jobs = new();
    private readonly FakeReportArtifactRepository artifacts = new();

    private (ReportingCommandService commands, ReportingQueryService queries) CreateServices(
        IReportRenderer? renderer = null)
    {
        // Arrange
        definitions.AddAsync(new ReportDefinition(
            new DefineReportIndicatorCommand("fatigue-by-shift", "Fatigue alerts by shift",
                "v1.0", ["alerts"]))).GetAwaiter().GetResult();
        return (new ReportingCommandService(definitions, jobs, artifacts,
                renderer ?? new InProcessReportRenderer(), TimeProvider.System),
            new ReportingQueryService(definitions, jobs, artifacts));
    }

    private static RequestReportGenerationCommand JobCommand() =>
        new(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            "site-north", "fleet-a", "fatigue-by-shift", ReportFormat.Pdf, Guid.NewGuid());

    [Fact]
    public async Task RequestJob_KnownDefinition_RendersArtifactAndIsReady()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var command = JobCommand();

        // Act
        var job = await commands.Handle(command);
        var artifact = await queries.FindArtifactByJobIdAsync(job.Id);

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        Assert.Equal(command.RequestedBy, job.RequestedBy);
        Assert.NotNull(job.CompletedAt);
        Assert.NotNull(artifact);
        Assert.Equal("application/pdf", artifact.ContentType);
        Assert.StartsWith("sha256:", artifact.Checksum);
        Assert.Equal($"/api/v1/reports/{job.Id}/download", artifact.Uri);
    }

    [Fact]
    public async Task RequestJob_SeveralVersions_UsesLatestDefinition()
    {
        // Arrange
        var (commands, _) = CreateServices();
        await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.10", ["alerts", "incidents"]));
        await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.9", ["alerts"]));

        // Act
        var job = await commands.Handle(JobCommand());

        // Assert
        Assert.Equal("v1.10", job.DefinitionVersion);
    }

    [Fact]
    public async Task RequestJob_RendererFails_MarksJobFailedWithoutArtifact()
    {
        // Arrange
        var (commands, queries) = CreateServices(new FailingRenderer());

        // Act
        var job = await commands.Handle(JobCommand());

        // Assert
        Assert.Equal(ReportJobStatus.Failed, job.Status);
        Assert.Equal("renderer unavailable", job.FailureReason);
        Assert.Null(await queries.FindArtifactByJobIdAsync(job.Id));
    }

    [Fact]
    public async Task RequestJob_WithoutRequester_ThrowsDomainException()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = JobCommand() with { RequestedBy = Guid.Empty };

        // Act
        Func<Task> act = () => commands.Handle(command);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task RequestJob_UnknownDefinition_ThrowsDomainException()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = JobCommand() with { DefinitionCode = "no-such-indicator" };

        // Act
        Func<Task> act = () => commands.Handle(command);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
    }

    [Fact]
    public async Task GetReportJobs_FiltersBySiteAndPaginates()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        await commands.Handle(JobCommand());
        await commands.Handle(JobCommand() with { SiteId = "site-south" });

        // Act
        var north = await queries.Handle(new GetReportJobsQuery("site-north", null, null, null, 1, 20));
        var page = await queries.Handle(new GetReportJobsQuery(null, null, null, null, 1, 1));

        // Assert
        Assert.Single(north);
        Assert.Single(page);
    }

    private sealed class FailingRenderer : IReportRenderer
    {
        public RenderedReport Render(ReportJob job, ReportDefinition definition, DateTimeOffset generatedAt) =>
            throw new InvalidOperationException("renderer unavailable");
    }
}
