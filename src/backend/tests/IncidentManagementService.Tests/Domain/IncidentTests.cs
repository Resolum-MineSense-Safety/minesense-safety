using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Tests.Domain;

public class IncidentTests
{
    private static readonly DateTimeOffset OpenedAt = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Supervisor = Guid.NewGuid();

    private static Incident Open(IncidentSeverity severity = IncidentSeverity.Critical) =>
        new(new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid(), severity), OpenedAt);

    private static void RecordPause(Incident incident) =>
        incident.RecordAction(IncidentActionType.PreventivePause, "Stop at the next bay.",
            "Operator rested 15 minutes.", Supervisor, OpenedAt.AddMinutes(1));

    [Fact]
    public void Constructor_WithValidCommand_StartsAsPendingWithHistory()
    {
        // Act
        var incident = Open();

        // Assert
        Assert.Equal(IncidentStatus.Pending, incident.Status);
        Assert.Equal(OpenedAt, incident.OpenedAt);
        Assert.Null(incident.AssigneeId);
        Assert.Equal(IncidentEventType.Opened, Assert.Single(incident.History).Type);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Constructor_WithoutOperatorOrAlert_Throws(bool emptyOperator, bool emptyAlert)
    {
        // Arrange
        var command = new OpenIncidentCommand(emptyOperator ? Guid.Empty : Guid.NewGuid(),
            emptyAlert ? Guid.Empty : Guid.NewGuid(), IncidentSeverity.Critical);

        // Act & Assert
        Assert.Throws<DomainException>(() => new Incident(command, OpenedAt));
    }

    [Fact]
    public void Assign_PendingIncident_RegistersResponsibleAndMoment()
    {
        // Arrange
        var incident = Open();
        var assignedAt = OpenedAt.AddSeconds(40);

        // Act
        incident.Assign(Supervisor, assignedAt);

        // Assert
        Assert.Equal(IncidentStatus.Assigned, incident.Status);
        Assert.Equal(Supervisor, incident.AssigneeId);
        Assert.Equal(assignedAt, incident.AssignedAt);
    }

    [Fact]
    public void Assign_BySameSupervisorTwice_IsIdempotent()
    {
        // Arrange
        var incident = Open();
        incident.Assign(Supervisor, OpenedAt.AddSeconds(10));

        // Act
        incident.Assign(Supervisor, OpenedAt.AddSeconds(20));

        // Assert
        Assert.Equal(OpenedAt.AddSeconds(10), incident.AssignedAt);
        Assert.Equal(2, incident.History.Count);
    }

    [Fact]
    public void Assign_AlreadyTakenByAnotherSupervisor_ThrowsConflictNamingCurrentResponsible()
    {
        // Arrange
        var incident = Open();
        incident.Assign(Supervisor, OpenedAt.AddSeconds(10));

        // Act
        var exception = Assert.Throws<DomainConflictException>(() => incident.Assign(Guid.NewGuid(), OpenedAt.AddSeconds(20)));

        // Assert
        Assert.Contains(Supervisor.ToString(), exception.Message);
        Assert.Equal(Supervisor, incident.AssigneeId);
    }

    [Fact]
    public void Assign_EmptySupervisor_Throws()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Open().Assign(Guid.Empty, OpenedAt));
    }

    [Fact]
    public void RecordAction_WithResult_AddsActionAndHistoryEntry()
    {
        // Arrange
        var incident = Open();

        // Act
        var action = incident.RecordAction(IncidentActionType.OperatorRelief, " Relief driver sent. ",
            " Operator replaced. ", Supervisor, OpenedAt.AddMinutes(3));

        // Assert
        Assert.Same(action, Assert.Single(incident.Actions));
        Assert.Equal("Relief driver sent.", action.Description);
        Assert.Equal("Operator replaced.", action.Result);
        Assert.Equal(IncidentEventType.ActionRecorded, incident.History[^1].Type);
    }

    [Theory]
    [InlineData("", "Result")]
    [InlineData("Description", " ")]
    public void RecordAction_WithIncompleteInformation_Throws(string description, string result)
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Open().RecordAction(IncidentActionType.Other, description, result,
            Supervisor, OpenedAt));
    }

    [Fact]
    public void RecordAction_WithoutPerformer_Throws()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Open().RecordAction(IncidentActionType.Other, "Called", "Answered",
            Guid.Empty, OpenedAt));
    }

    [Fact]
    public void RecordAction_BeforeOpening_Throws()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Open().RecordAction(IncidentActionType.Other, "Called", "Answered",
            Supervisor, OpenedAt.AddMinutes(-1)));
    }

    [Fact]
    public void Close_WithRecordedAction_StoresResolutionAndAttentionTime()
    {
        // Arrange
        var incident = Open();
        incident.Assign(Supervisor, OpenedAt.AddSeconds(20));
        RecordPause(incident);

        // Act
        incident.Close("Operator recovered.", OpenedAt.AddMinutes(20));

        // Assert
        Assert.Equal(IncidentStatus.Closed, incident.Status);
        Assert.Equal("Operator recovered.", incident.Resolution);
        Assert.Equal(TimeSpan.FromMinutes(20), incident.AttentionTime);
        Assert.Equal(IncidentEventType.Closed, incident.History[^1].Type);
    }

    [Fact]
    public void Close_WithoutActions_ThrowsConflict()
    {
        // Act & Assert
        Assert.Throws<DomainConflictException>(() => Open().Close("Done", OpenedAt.AddMinutes(1)));
    }

    [Fact]
    public void Close_WithoutResolution_Throws()
    {
        // Arrange
        var incident = Open();
        RecordPause(incident);

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.Close(" ", OpenedAt.AddMinutes(5)));
    }

    [Fact]
    public void Close_BeforeOpening_Throws()
    {
        // Arrange
        var incident = Open();
        RecordPause(incident);

        // Act & Assert
        Assert.Throws<DomainException>(() => incident.Close("Done", OpenedAt.AddMinutes(-5)));
    }

    [Fact]
    public void ClosedIncident_RejectsNewActionsAssignmentsAndClosures()
    {
        // Arrange
        var incident = Open();
        RecordPause(incident);
        incident.Close("Done", OpenedAt.AddMinutes(5));

        // Act & Assert
        Assert.Throws<DomainConflictException>(() => RecordPause(incident));
        Assert.Throws<DomainConflictException>(() => incident.Assign(Supervisor, OpenedAt.AddMinutes(6)));
        Assert.Throws<DomainConflictException>(() => incident.Close("Again", OpenedAt.AddMinutes(7)));
    }

    [Fact]
    public void IsEscalationDue_CriticalUnattendedAfterDeadline_IsTrue()
    {
        // Arrange
        var incident = Open();
        var justAfter = OpenedAt + IncidentEscalationPolicy.AttentionDeadline + TimeSpan.FromSeconds(1);

        // Act & Assert
        Assert.False(incident.IsEscalationDue(OpenedAt + IncidentEscalationPolicy.AttentionDeadline));
        Assert.True(incident.IsEscalationDue(justAfter));
    }

    [Fact]
    public void IsEscalationDue_AssignedInTime_IsFalse()
    {
        // Arrange
        var incident = Open();
        incident.Assign(Supervisor, OpenedAt.AddSeconds(30));

        // Act & Assert
        Assert.False(incident.IsEscalationDue(OpenedAt.AddHours(1)));
    }

    [Fact]
    public void IsEscalationDue_WarningIncident_IsNotApplicable()
    {
        // Arrange
        var incident = Open(IncidentSeverity.Warning);

        // Act & Assert
        Assert.False(incident.IsEscalationApplicable);
        Assert.False(incident.IsEscalationDue(OpenedAt.AddHours(1)));
    }

    [Fact]
    public void Escalate_RepeatedlyUnattended_ClimbsChainUntilLastLevel()
    {
        // Arrange
        var incident = Open();
        var step = IncidentEscalationPolicy.AttentionDeadline + TimeSpan.FromSeconds(1);

        // Act
        incident.Escalate(OpenedAt + step);
        var dueRightAfterFirst = incident.IsEscalationDue(OpenedAt + step + TimeSpan.FromSeconds(1));
        incident.Escalate(OpenedAt + step * 2);

        // Assert
        Assert.False(dueRightAfterFirst);
        Assert.Equal(IncidentEscalationPolicy.MaxLevel, incident.EscalationLevel);
        Assert.Equal(OpenedAt + step * 2, incident.LastEscalatedAt);
        Assert.False(incident.IsEscalationDue(OpenedAt.AddHours(1)));
        Assert.Equal(2, incident.History.Count(e => e.Type == IncidentEventType.Escalated));
    }

    [Fact]
    public void Escalate_WhenNotDue_Throws()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Open().Escalate(OpenedAt.AddSeconds(5)));
    }

    [Fact]
    public void EscalationPolicy_NotifiedRoles_GrowWithLevel()
    {
        // Act & Assert
        Assert.Empty(IncidentEscalationPolicy.NotifiedRolesAt(0));
        Assert.Equal(["ShiftManager"], IncidentEscalationPolicy.NotifiedRolesAt(1));
        Assert.Equal(["ShiftManager", "SafetyManager"], IncidentEscalationPolicy.NotifiedRolesAt(5));
    }
}
