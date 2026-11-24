using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.Queries;

namespace ReportingService.Tests.Application;

public class DefinitionServicesTests
{
    private static (ReportingCommandService commands, ReportingQueryService queries) CreateServices()
    {
        // Arrange
        var definitions = new FakeReportDefinitionRepository();
        return (new ReportingCommandService(definitions, null!, null!, TimeProvider.System),
            new ReportingQueryService(definitions, null!, null!));
    }

    [Fact]
    public async Task DefineIndicator_NewCode_ReturnsCreated()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["alerts"]);

        // Act
        var (definition, created) = await commands.Handle(command);

        // Assert
        Assert.True(created);
        Assert.NotEqual(Guid.Empty, definition.Id);
    }

    [Fact]
    public async Task DefineIndicator_SameCodeAndVersion_ReturnsExisting()
    {
        // Arrange
        var (commands, _) = CreateServices();
        var command = new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["alerts"]);

        // Act
        var (first, firstCreated) = await commands.Handle(command);
        var (second, secondCreated) = await commands.Handle(command);

        // Assert
        Assert.True(firstCreated);
        Assert.False(secondCreated);
        Assert.Equal(first.Id, second.Id);
    }

    [Fact]
    public async Task DefineIndicator_SameCodeNewVersion_CreatesNew()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["alerts"]));

        // Act
        var (second, created) = await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v2.0", ["alerts", "incidents"]));
        var all = await queries.Handle(new GetReportDefinitionsQuery(true));

        // Assert
        Assert.True(created);
        Assert.Equal(2, all.Count());
    }
}
