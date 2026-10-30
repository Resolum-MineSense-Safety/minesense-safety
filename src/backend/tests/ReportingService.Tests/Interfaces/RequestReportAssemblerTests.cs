using ReportingService.Interfaces.REST.Resources;
using ReportingService.Interfaces.REST.Transform;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Tests.Interfaces;

public class RequestReportAssemblerTests
{
    private static RequestSafetyReportResource Resource(Guid? requestedBy) =>
        new(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, null, null,
            "fatigue-by-shift", "Pdf", requestedBy);

    [Fact]
    public void ToCommand_AuthenticatedCaller_UsesTokenOverBody()
    {
        // Arrange
        var tokenUser = Guid.NewGuid();

        // Act
        var command = RequestReportGenerationCommandFromResourceAssembler
            .ToCommandFromResource(Resource(Guid.NewGuid()), tokenUser);

        // Assert
        Assert.Equal(tokenUser, command.RequestedBy);
    }

    [Fact]
    public void ToCommand_AnonymousCaller_UsesBody()
    {
        // Arrange
        var bodyUser = Guid.NewGuid();

        // Act
        var command = RequestReportGenerationCommandFromResourceAssembler
            .ToCommandFromResource(Resource(bodyUser));

        // Assert
        Assert.Equal(bodyUser, command.RequestedBy);
    }

    [Fact]
    public void ToCommand_WithoutAnyRequester_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() =>
            RequestReportGenerationCommandFromResourceAssembler.ToCommandFromResource(Resource(null)));
    }
}
