using System.Security.Cryptography;
using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;
using ReportingService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Tests.Domain;

public class ReportJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);

    private static ReportJob NewJob() =>
        new(new RequestReportGenerationCommand(Now.AddDays(-7), Now, "site-north", null,
            "fatigue-by-shift", ReportFormat.Csv, Guid.NewGuid()), "v1.0", Now);

    [Fact]
    public void Lifecycle_RequestedRenderingReady_SetsCompletion()
    {
        // Arrange
        var job = NewJob();

        // Act
        job.StartRendering();
        job.MarkReady(Now.AddSeconds(2));

        // Assert
        Assert.Equal(ReportJobStatus.Ready, job.Status);
        Assert.Equal(Now.AddSeconds(2), job.CompletedAt);
    }

    [Fact]
    public void MarkReady_WithoutRendering_ThrowsDomainException()
    {
        // Arrange
        var job = NewJob();

        // Act & Assert
        Assert.Throws<DomainException>(() => job.MarkReady(Now));
    }

    [Fact]
    public void StartRendering_Twice_ThrowsDomainException()
    {
        // Arrange
        var job = NewJob();
        job.StartRendering();

        // Act & Assert
        Assert.Throws<DomainException>(() => job.StartRendering());
    }

    [Fact]
    public void Constructor_WithoutRequester_ThrowsDomainException()
    {
        // Arrange
        var command = new RequestReportGenerationCommand(Now.AddDays(-1), Now, null, null,
            "fatigue-by-shift", ReportFormat.Pdf, Guid.Empty);

        // Act & Assert
        Assert.Throws<DomainException>(() => new ReportJob(command, "v1.0", Now));
    }

    [Fact]
    public void Artifact_ComputesSha256ChecksumAndExpires()
    {
        // Arrange
        var content = "field,value\n"u8.ToArray();
        var expected = "sha256:" + Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        // Act
        var artifact = new ReportArtifact(Guid.NewGuid(), "/api/v1/reports/x/download", "x.csv",
            "text/csv", content, Now.AddDays(7));

        // Assert
        Assert.Equal(expected, artifact.Checksum);
        Assert.False(artifact.IsExpired(Now));
        Assert.True(artifact.IsExpired(Now.AddDays(7)));
    }

    [Fact]
    public void Artifact_WithoutContent_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => new ReportArtifact(Guid.NewGuid(), "/download", "x.csv",
            "text/csv", [], Now));
    }

    [Theory]
    [InlineData("v2.0", "v1.10")]
    [InlineData("v1.10", "v1.9")]
    [InlineData("2", "v1.5")]
    [InlineData("v1.0", "draft")]
    public void VersionComparer_OrdersNumerically(string newer, string older)
    {
        // Act & Assert
        Assert.True(ReportDefinitionVersionComparer.Instance.Compare(newer, older) > 0);
        Assert.True(ReportDefinitionVersionComparer.Instance.Compare(older, newer) < 0);
    }
}
