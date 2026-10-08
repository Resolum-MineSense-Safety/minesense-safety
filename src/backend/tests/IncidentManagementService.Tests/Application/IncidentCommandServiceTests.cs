using IncidentManagementService.Application.Internal.CommandServices;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Infrastructure.Persistence.InMemory;

namespace IncidentManagementService.Tests.Application;

public class IncidentCommandServiceTests
{
    private readonly InMemoryIncidentRepository _repository = new();
    private readonly IncidentCommandService _service;

    public IncidentCommandServiceTests()
    {
        _service = new IncidentCommandService(_repository, TimeProvider.System);
    }

    [Fact]
    public async Task Handle_OpenIncidentCommand_PersistsIncident()
    {
        // Arrange
        var command = new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var incident = await _service.Handle(command);

        // Assert
        Assert.NotNull(await _repository.FindByIdAsync(incident.Id));
    }

    [Fact]
    public async Task Handle_FullLifecycle_ClosesIncidentWithAuditTrail()
    {
        // Arrange
        var supervisorId = Guid.NewGuid();
        var incident = await _service.Handle(new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid()));

        // Act
        await _service.Handle(new AssignIncidentCommand(incident.Id, supervisorId));
        await _service.Handle(new RegisterIncidentActionCommand(
            incident.Id, supervisorId, "Operator relieved", "Relief operator took over"));
        var closed = await _service.Handle(new CloseIncidentCommand(incident.Id, "Risk mitigated"));

        // Assert
        Assert.NotNull(closed);
        Assert.Equal(IncidentStatus.Closed, closed.Status);
        Assert.Single(closed.Actions);
    }

    [Fact]
    public async Task Handle_AssignUnknownIncident_ReturnsNull()
    {
        // Arrange
        var command = new AssignIncidentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var incident = await _service.Handle(command);

        // Assert
        Assert.Null(incident);
    }
}
