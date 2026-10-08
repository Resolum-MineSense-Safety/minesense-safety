using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Tests.Domain;

public class IncidentTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid SupervisorId = Guid.NewGuid();

    private static Incident OpenIncident() =>
        new(new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid()), OpenedAt);

    private static Incident AssignedIncident()
    {
        var incident = OpenIncident();
        incident.Assign(SupervisorId, OpenedAt.AddMinutes(2));
        return incident;
    }

    private static IncidentAction ValidAction(Guid supervisorId) =>
        new(supervisorId, "Operator relieved", "Relief operator took over", OpenedAt.AddMinutes(10));

    [Fact]
    public void Constructor_WithValidCommand_StartsAsPending()
    {
        // Arrange
        var command = new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var incident = new Incident(command, OpenedAt);

        // Assert
        Assert.Equal(IncidentStatus.Pending, incident.Status);
        Assert.Equal(OpenedAt, incident.OpenedAt);
        Assert.Empty(incident.Actions);
    }

    [Fact]
    public void Constructor_WithoutAlert_ThrowsDomainException()
    {
        // Arrange
        var command = new OpenIncidentCommand(Guid.Empty, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<DomainException>(() => new Incident(command, OpenedAt));
    }

    [Fact]
    public void Assign_PendingIncident_SetsResponsibleAndAssignmentTime()
    {
        // Arrange
        var incident = OpenIncident();
        var assignedAt = OpenedAt.AddMinutes(2);

        // Act
        incident.Assign(SupervisorId, assignedAt);

        // Assert
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(SupervisorId, incident.AssignedSupervisorId);
        Assert.Equal(assignedAt, incident.AssignedAt);
    }

    [Fact]
    public void Assign_AlreadyAssignedIncident_ThrowsDomainException()
    {
        // Arrange
        var incident = AssignedIncident();

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.Assign(Guid.NewGuid(), OpenedAt.AddMinutes(5)));
    }

    [Fact]
    public void Assign_EscalatedIncident_ReassignsSupervisor()
    {
        // Arrange
        var incident = AssignedIncident();
        incident.Escalate();
        var seniorSupervisorId = Guid.NewGuid();

        // Act
        incident.Assign(seniorSupervisorId, OpenedAt.AddMinutes(20));

        // Assert
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(seniorSupervisorId, incident.AssignedSupervisorId);
    }

    [Fact]
    public void RegisterAction_ByAnotherSupervisor_ThrowsDomainException()
    {
        // Arrange
        var incident = AssignedIncident();

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.RegisterAction(ValidAction(Guid.NewGuid())));
    }

    [Fact]
    public void RegisterAction_OnPendingIncident_ThrowsDomainException()
    {
        // Arrange
        var incident = OpenIncident();

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.RegisterAction(ValidAction(SupervisorId)));
    }

    [Fact]
    public void IncidentAction_WithoutOutcome_ThrowsDomainException()
    {
        // Arrange
        var registeredAt = OpenedAt.AddMinutes(10);

        // Act & Assert
        Assert.Throws<DomainException>(() => new IncidentAction(SupervisorId, "Operator relieved", " ", registeredAt));
    }

    [Fact]
    public void Escalate_PendingIncident_ChangesStatusToEscalated()
    {
        // Arrange
        var incident = OpenIncident();

        // Act
        incident.Escalate();

        // Assert
        Assert.Equal(IncidentStatus.Escalated, incident.Status);
    }

    [Fact]
    public void Close_WithoutActions_ThrowsDomainException()
    {
        // Arrange
        var incident = AssignedIncident();

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.Close("Resolved", OpenedAt.AddMinutes(30)));
    }

    [Fact]
    public void Close_WithoutResolution_ThrowsDomainException()
    {
        // Arrange
        var incident = AssignedIncident();
        incident.RegisterAction(ValidAction(SupervisorId));

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.Close("", OpenedAt.AddMinutes(30)));
    }

    [Fact]
    public void Close_AssignedIncidentWithAction_RecordsResolutionAndClosingTime()
    {
        // Arrange
        var incident = AssignedIncident();
        incident.RegisterAction(ValidAction(SupervisorId));
        var closedAt = OpenedAt.AddMinutes(30);

        // Act
        incident.Close("Operator rested before next shift", closedAt);

        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("Operator rested before next shift", incident.Resolution);
        Assert.Equal(closedAt, incident.ClosedAt);
    }

    [Fact]
    public void Escalate_ClosedIncident_ThrowsDomainException()
    {
        // Arrange
        var incident = AssignedIncident();
        incident.RegisterAction(ValidAction(SupervisorId));
        incident.Close("Resolved", OpenedAt.AddMinutes(30));

        // Act & Assert
        Assert.Throws<DomainException>(incident.Escalate);
    }
}
