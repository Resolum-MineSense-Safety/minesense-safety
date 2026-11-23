using System.Text;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Infrastructure.Persistence.InMemory;
using ReportingService.Infrastructure.Rendering;

namespace ReportingService.Tests.Infrastructure;

public class ReportingInfrastructureTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static ReportDefinition Definition(string version) =>
        new(new DefineReportIndicatorCommand("fatigue-by-shift", "Fatigue alerts by shift",
            version, ["alerts", "incidents"]));

    private static ReportJob Job(ReportFormat format) =>
        new(new RequestReportGenerationCommand(Now.AddDays(-7), Now, "site-north", "fleet-a",
            "fatigue-by-shift", format, Guid.NewGuid()), "v1.0", Now);

    [Fact]
    public void Render_Pdf_ProducesWellFormedDocument()
    {
        // Arrange
        var job = Job(ReportFormat.Pdf);

        // Act
        var rendered = new InProcessReportRenderer().Render(job, Definition("v1.0"), Now);
        var text = Encoding.ASCII.GetString(rendered.Content);

        // Assert
        Assert.Equal("application/pdf", rendered.ContentType);
        Assert.EndsWith(".pdf", rendered.FileName);
        Assert.StartsWith("%PDF-1.4", text);
        Assert.EndsWith("%%EOF\n", text);
        Assert.Contains("site-north", text);
    }

    [Fact]
    public void Render_Csv_ListsJobParameters()
    {
        // Arrange
        var job = Job(ReportFormat.Csv);

        // Act
        var rendered = new InProcessReportRenderer().Render(job, Definition("v1.0"), Now);
        var lines = Encoding.UTF8.GetString(rendered.Content).Split('\n');

        // Assert
        Assert.Equal("text/csv", rendered.ContentType);
        Assert.Equal("field,value", lines[0]);
        Assert.Contains($"reportJobId,{job.Id}", lines);
        Assert.Contains("dataSources,alerts incidents", lines);
        Assert.Contains($"requestedBy,{job.RequestedBy}", lines);
    }

    [Fact]
    public async Task DefinitionRepository_FindByCode_ReturnsLatestVersion()
    {
        // Arrange
        var repository = new InMemoryReportDefinitionRepository();
        await repository.AddAsync(Definition("v1.2"));
        await repository.AddAsync(Definition("v1.10"));
        await repository.AddAsync(Definition("v1.9"));

        // Act
        var latest = await repository.FindByCodeAsync("FATIGUE-BY-SHIFT");
        var exact = await repository.FindByCodeAndVersionAsync("fatigue-by-shift", "v1.2");

        // Assert
        Assert.Equal("v1.10", latest!.Version);
        Assert.Equal("v1.2", exact!.Version);
    }
}
