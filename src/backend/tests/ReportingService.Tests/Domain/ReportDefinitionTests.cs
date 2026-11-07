using ReportingService.Domain.Model.Aggregates;
using ReportingService.Domain.Model.Commands;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Tests.Domain;

public class ReportDefinitionTests
{
    [Fact]
    public void Constructor_WithValidData_StartsActive()
    {
        // Arrange
        var command = new DefineReportIndicatorCommand("fatigue-by-shift",
            "Fatigue alerts by shift", "v1.0", ["fatigue-assessments", "alerts"]);

        // Act
        var definition = new ReportDefinition(command);

        // Assert
        Assert.Equal("fatigue-by-shift", definition.Code);
        Assert.True(definition.IsActive);
    }

    [Fact]
    public void Constructor_WithBlankCode_ThrowsDomainException()
    {
        // Arrange
        var command = new DefineReportIndicatorCommand("  ",
            "Fatigue alerts by shift", "v1.0", ["alerts"]);

        // Act
        Action act = () => new ReportDefinition(command);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}
