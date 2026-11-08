using ReportingService.Application.Internal.CommandServices;
using ReportingService.Application.Internal.QueryServices;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.Queries;
using ReportingService.Infrastructure.Rendering;

namespace ReportingService.Tests.Application;

public class DefinitionServicesTests
{
    private static (ReportingCommandService commands, ReportingQueryService queries) CreateServices()
    {
        // Arrange
        var definitions = new FakeReportDefinitionRepository();
        return (new ReportingCommandService(definitions, null!, null!, new InProcessReportRenderer(),
                TimeProvider.System),
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

    [Fact]
    public async Task DefineIndicator_OlderVersionAgainAfterNewer_ReturnsExisting()
    {
        // Arrange
        var (commands, queries) = CreateServices();
        var (original, _) = await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["alerts"]));
        await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v2.0", ["alerts", "incidents"]));

        // Act
        var (again, created) = await commands.Handle(new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["alerts"]));
        var all = await queries.Handle(new GetReportDefinitionsQuery(true));

        // Assert
        Assert.False(created);
        Assert.Equal(original.Id, again.Id);
        Assert.Equal(2, all.Count());
    }
}
