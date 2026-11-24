using PrivacyAuditService.Application.Internal.CommandServices;
using PrivacyAuditService.Application.Internal.QueryServices;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.Queries;

namespace PrivacyAuditService.Tests.Application;

public class ReportServicesTests
{
    private static (PrivacyAuditCommandService commands, PrivacyAuditQueryService queries) CreateServices()
    {
        // Arrange
        var reports = new FakeComplianceReportRepository();
        return (new PrivacyAuditCommandService(new FakeProcessingConsentRepository(),
                new FakeAccessAuditRepository(), reports, TimeProvider.System),
            new PrivacyAuditQueryService(new FakeProcessingConsentRepository(),
                new FakeAccessAuditRepository(), reports));
    }

    [Fact]
    public async Task RequestReport_StoresAndRetrieves()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var command = new RequestSafetyReportCommand(
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero),
            "site-north", "fleet-a", Guid.NewGuid());

        // Act
        var created = await commands.Handle(command);
        var found = await queries.Handle(new GetReportByIdQuery(created.Id));

        // Assert
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal("site-north", found.SiteId);
    }

    [Fact]
    public async Task GetReportById_UnknownId_ReturnsNull()
    {
        // Arrange
        var (_, queries) = CreateServices();

        // Act
        var found = await queries.Handle(new GetReportByIdQuery(Guid.NewGuid()));

        // Assert
        Assert.Null(found);
    }
}
