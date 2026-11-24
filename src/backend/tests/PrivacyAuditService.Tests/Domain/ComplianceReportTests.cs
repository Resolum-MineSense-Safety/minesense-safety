using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Tests.Domain;

public class ComplianceReportTests
{
    [Fact]
    public void Constructor_WithValidPeriod_StartsRequested()
    {
        // Arrange
        var command = new RequestSafetyReportCommand(
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            "site-north", null, Guid.NewGuid());

        // Act
        var report = new ComplianceReport(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(ReportStatus.Requested, report.Status);
    }

    [Fact]
    public void Constructor_WithInvertedPeriod_ThrowsDomainException()
    {
        // Arrange
        var command = new RequestSafetyReportCommand(
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            null, null, Guid.NewGuid());

        // Act
        Action act = () => new ComplianceReport(command, DateTimeOffset.UtcNow);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}
