using MineSenseSafety.Shared.Domain.Model;
using MineSenseSafety.Shared.Infrastructure.Persistence.MongoDb;
using MineSenseSafety.Tests.Support;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Infrastructure.Persistence.MongoDb;

namespace ReportingService.Tests.Infrastructure;

public class MongoReportingRepositoriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static ReportDefinition Definition(string code, string version) =>
        new(new DefineReportIndicatorCommand(code, "Fatigue alerts by shift", version, ["alerts", "incidents"]));

    private static ReportJob Job(DateTimeOffset requestedAt) =>
        new(new RequestReportGenerationCommand(Now.AddDays(-7), Now, "site-north", "fleet-a",
            "fatigue-by-shift", ReportFormat.Csv, Guid.NewGuid()), "v1.0", requestedAt);

    [Fact]
    public void Snapshot_RoundTrip_KeepsArtifactBytesAndChecksum()
    {
        // Arrange
        var artifact = new ReportArtifact(Guid.NewGuid(), "/api/v1/reports/x/download", "report.csv", "text/csv",
            [1, 2, 3, 250], Now.AddDays(7));

        // Act
        var restored = AggregateSnapshot.Deserialize<ReportArtifact>(AggregateSnapshot.Serialize(artifact));

        // Assert
        Assert.Equal(artifact.Content.ToArray(), restored.Content.ToArray());
        Assert.Equal(artifact.Checksum, restored.Checksum);
        Assert.Equal(artifact.ExpiresAt, restored.ExpiresAt);
    }

    [MongoFact]
    public async Task Mongo_RealServer_DefinitionsJobsAndArtifactsSurviveRestart()
    {
        // Arrange
        await using var mongo = new MongoTestDatabase("reporting_tests");
        var definitions = new MongoReportDefinitionRepository(mongo.Database);
        var jobs = new MongoReportJobRepository(mongo.Database);
        var artifacts = new MongoReportArtifactRepository(mongo.Database);
        foreach (var repository in new IMongoCollectionInitializer[] { definitions, jobs, artifacts })
            await repository.InitializeAsync(CancellationToken.None);
        var job = Job(Now);
        var artifact = new ReportArtifact(job.Id, $"/api/v1/reports/{job.Id}/download", "report.csv", "text/csv",
            [65, 66, 67], Now.AddDays(7));

        // Act
        await definitions.AddAsync(Definition("fatigue-by-shift", "v1.2"));
        await definitions.AddAsync(Definition("fatigue-by-shift", "v1.10"));
        await definitions.AddAsync(Definition("other", "v9.0"));
        await Assert.ThrowsAsync<DomainConflictException>(() => definitions.AddAsync(Definition("FATIGUE-BY-SHIFT", "V1.2")));
        await jobs.AddAsync(job);
        job.StartRendering();
        await jobs.UpdateAsync(job);
        await artifacts.AddAsync(artifact);
        await Assert.ThrowsAsync<DomainConflictException>(() => artifacts.AddAsync(new ReportArtifact(job.Id,
            "/again", "again.csv", "text/csv", [1], Now.AddDays(7))));
        var database = mongo.Reopen();

        // Assert
        Assert.Equal("v1.10", (await new MongoReportDefinitionRepository(database).FindByCodeAsync("Fatigue-By-Shift"))!.Version);
        Assert.NotNull(await new MongoReportDefinitionRepository(database).FindByCodeAndVersionAsync("fatigue-by-shift", "V1.2"));
        Assert.Null(await new MongoReportDefinitionRepository(database).FindByCodeAsync("missing"));
        Assert.Equal(job.Status, (await new MongoReportJobRepository(database).FindByIdAsync(job.Id))!.Status);
        var stored = await new MongoReportArtifactRepository(database).FindByReportJobIdAsync(job.Id);
        Assert.Equal("ABC"u8.ToArray(), stored!.Content.ToArray());
    }
}
