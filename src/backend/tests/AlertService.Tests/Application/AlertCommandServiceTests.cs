using AlertService.Application.Internal.CommandServices;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Infrastructure.Persistence.InMemory;

namespace AlertService.Tests.Application;

public class AlertCommandServiceTests
{
    private readonly InMemoryAlertRepository _repository = new();
    private readonly AlertCommandService _service;

    public AlertCommandServiceTests()
    {
        _service = new AlertCommandService(_repository, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_IssueAlertCommand_PersistsAlert()
    {
        // Arrange
        var command = new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Critical);

        // Act
        var alert = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(alert.Id));
    }

    [Fact]
    public async Task Handle_AcknowledgeUnknownAlert_ReturnsNull()
    {
        // Arrange
        var command = new AcknowledgeAlertCommand(Guid.NewGuid());

        // Act
        var alert = await _service.Handle(command);

        // Assert
        Assert.Null(alert);
    }
}
