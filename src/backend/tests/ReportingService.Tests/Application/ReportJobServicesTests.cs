using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Tests.Application;

public class ReportJobServicesTests
{
    private static (ReportingCommandService commands, ReportingQueryService queries) CreateServices()
    {
        // Arrange
        var definitions = new FakeReportDefinitionRepository();
        var jobs = new FakeReportJobRepository();
        definitions.AddAsync(new ReportingService.Domain.Model.Aggregates.ReportDefinition(
            new DefineReportIndicatorCommand("fatigue-by-shift", "Fatigue alerts by shift",
                "v1.0", ["alerts"]))).GetAwaiter().GetResult();
        return (new ReportingCommandService(definitions, jobs, null!, TimeProvider.System),
            new ReportingQueryService(definitions, jobs, null!));
    }

    private static RequestReportGenerationCommand JobCommand() =>
        new(new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            "site-north", "fleet-a", "fatigue-by-shift", ReportFormat.Pdf, Guid.NewGuid());

    [Fact]
    public async Task RequestJob_KnownDefinition_ReturnsRequested()
    {
        // Arrange
        var (commands, _) = CreateServices();

        // Act
        var job = await commands.Handle(JobCommand());

        // Assert
        Assert.Equal(ReportJobStatus.Requested, job.Status);
        Assert.Equal("fatigue-by-shift", job.DefinitionCode);
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
}
