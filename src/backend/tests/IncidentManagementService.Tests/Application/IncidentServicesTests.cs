using IncidentManagementService.Application.Internal.CommandServices;
using IncidentManagementService.Application.Internal.QueryServices;
using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.Queries;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Services;

namespace IncidentManagementService.Tests.Application;

public class IncidentServicesTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
    private static readonly Guid Supervisor = Guid.NewGuid();
    private readonly FakeIncidentRepository _repository = new();
    private readonly FakeTimeProvider _clock = new(Start);
    private readonly IncidentCommandService _commandService;
    private readonly IncidentQueryService _queryService;

    public IncidentServicesTests()
    {
        _commandService = new IncidentCommandService(_repository, _clock);
        _queryService = new IncidentQueryService(_repository);
    }

    private Task<Incident> OpenAsync(IncidentSeverity severity = IncidentSeverity.Critical, Guid? operatorId = null) =>
        _commandService.Handle(new OpenIncidentCommand(operatorId ?? Guid.NewGuid(), Guid.NewGuid(), severity));

    [Fact]
    public async Task Handle_OpenIncidentCommand_PersistsIncidentAtCurrentTime()
    {
        // Act
        var incident = await OpenAsync();

        // Assert
        Assert.Same(incident, await _queryService.Handle(new GetIncidentByIdQuery(incident.Id)));
        Assert.Equal(Start, incident.OpenedAt);
    }

    [Fact]
    public async Task Handle_OpenIncidentCommand_ForSameAlert_ReturnsExistingIncident()
    {
        // Arrange
        var command = new OpenIncidentCommand(Guid.NewGuid(), Guid.NewGuid(), IncidentSeverity.Critical);
        var first = await _commandService.Handle(command);

        // Act
        var second = await _commandService.Handle(command);

        // Assert
        Assert.Same(first, second);
        Assert.Single(_repository.Items);
    }

    [Fact]
    public async Task Handle_AssignRecordAndClose_RunsFullLifecycle()
    {
        // Arrange
        var incident = await OpenAsync();

        // Act
        _clock.Now = Start.AddSeconds(30);
        await _commandService.Handle(new AssignIncidentCommand(incident.Id, Supervisor));
        var action = await _commandService.Handle(new RecordIncidentActionCommand(incident.Id,
            IncidentActionType.OperatorContact, "Radio call", "Operator answered", Supervisor, Start.AddMinutes(1)));
        _clock.Now = Start.AddMinutes(10);
        var closed = await _commandService.Handle(new CloseIncidentCommand(incident.Id, "Operator fine", null));

        // Assert
        Assert.NotNull(action);
        Assert.Equal(IncidentStatus.Closed, closed!.Status);
        Assert.Equal(Start.AddMinutes(10), closed.ClosedAt);
        Assert.Equal(3, _repository.Updates);
    }

    [Fact]
    public async Task Handle_CloseIncidentCommand_UsesGivenClosedAt()
    {
        // Arrange
        var incident = await OpenAsync();
        await _commandService.Handle(new RecordIncidentActionCommand(incident.Id,
            IncidentActionType.PreventivePause, "Pause", "Rested", Supervisor, Start.AddMinutes(1)));

        // Act
        var closed = await _commandService.Handle(new CloseIncidentCommand(incident.Id, "Ok", Start.AddMinutes(4)));

        // Assert
        Assert.Equal(Start.AddMinutes(4), closed!.ClosedAt);
    }

    [Fact]
    public async Task Handle_CommandsOnUnknownIncident_ReturnNull()
    {
        // Arrange
        var unknown = Guid.NewGuid();

        // Act & Assert
        Assert.Null(await _commandService.Handle(new AssignIncidentCommand(unknown, Supervisor)));
        Assert.Null(await _commandService.Handle(new RecordIncidentActionCommand(unknown,
            IncidentActionType.Other, "x", "y", Supervisor, Start)));
        Assert.Null(await _commandService.Handle(new CloseIncidentCommand(unknown, "x", null)));
        Assert.Equal(0, _repository.Updates);
    }

    [Fact]
    public async Task Handle_EscalateOverdueIncidents_OnlyEscalatesUnattendedCriticalPastDeadline()
    {
        // Arrange
        var overdue = await OpenAsync();
        var attended = await OpenAsync();
        var warning = await OpenAsync(IncidentSeverity.Warning);
        await _commandService.Handle(new AssignIncidentCommand(attended.Id, Supervisor));
        _clock.Now = Start + IncidentEscalationPolicy.AttentionDeadline + TimeSpan.FromSeconds(1);
        var recent = await OpenAsync();

        // Act
        var escalated = (await _commandService.Handle(new EscalateOverdueIncidentsCommand())).ToList();

        // Assert
        Assert.Same(overdue, Assert.Single(escalated));
        Assert.Equal(1, overdue.EscalationLevel);
        Assert.Equal(0, attended.EscalationLevel);
        Assert.Equal(0, warning.EscalationLevel);
        Assert.Equal(0, recent.EscalationLevel);
    }

    [Fact]
    public async Task Handle_GetIncidentsQuery_FiltersByStatusAndOperator()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var assigned = await OpenAsync(operatorId: operatorId);
        await OpenAsync(operatorId: operatorId);
        await OpenAsync();
        await _commandService.Handle(new AssignIncidentCommand(assigned.Id, Supervisor));

        // Act
        var byOperator = await _queryService.Handle(new GetIncidentsQuery(null, operatorId));
        var pendingOfOperator = await _queryService.Handle(new GetIncidentsQuery(IncidentStatus.Pending, operatorId));
        var allPending = await _queryService.Handle(new GetIncidentsQuery(IncidentStatus.Pending, null));

        // Assert
        Assert.Equal(2, byOperator.Count());
        Assert.Single(pendingOfOperator);
        Assert.Equal(2, allPending.Count());
    }
}
