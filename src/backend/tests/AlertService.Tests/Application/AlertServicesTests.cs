using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.QueryServices;
using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Model.ValueObjects;

namespace AlertService.Tests.Application;

public class AlertServicesTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private readonly FakeAlertRepository _repository = new();
    private readonly FakeTimeProvider _clock = new(Start);
    private readonly AlertCommandService _commandService;
    private readonly AlertQueryService _queryService;

    public AlertServicesTests()
    {
        _commandService = new AlertCommandService(_repository, _clock);
        _queryService = new AlertQueryService(_repository);
    }

    private Task<Alert> IssueAsync(AlertSeverity severity, Guid? operatorId = null) =>
        _commandService.Handle(new IssueAlertCommand(operatorId ?? Guid.NewGuid(), Guid.NewGuid(), severity));

    [Fact]
    public async Task Handle_IssueAlertCommand_PersistsAlert()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        // Act
        var alert = await IssueAsync(AlertSeverity.Critical, operatorId);

        // Assert
        Assert.Same(alert, await _queryService.Handle(new GetAlertByIdQuery(alert.Id)));
        Assert.Single(await _queryService.Handle(new GetAlertsByOperatorIdQuery(operatorId)));
    }

    [Fact]
    public async Task Handle_AcknowledgeAndEscalate_UpdateExistingAlerts()
    {
        // Arrange
        var toAcknowledge = await IssueAsync(AlertSeverity.Warning);
        var toEscalate = await IssueAsync(AlertSeverity.Warning);

        // Act
        var acknowledged = await _commandService.Handle(new AcknowledgeAlertCommand(toAcknowledge.Id));
        var escalated = await _commandService.Handle(new EscalateAlertCommand(toEscalate.Id));

        // Assert
        Assert.Equal(AlertStatus.Acknowledged, acknowledged!.Status);
        Assert.Equal(AlertStatus.Escalated, escalated!.Status);
        Assert.Equal(2, _repository.Updates);
    }

    [Fact]
    public async Task Handle_ActionsOnUnknownAlert_ReturnNull()
    {
        // Act
        var acknowledged = await _commandService.Handle(new AcknowledgeAlertCommand(Guid.NewGuid()));
        var escalated = await _commandService.Handle(new EscalateAlertCommand(Guid.NewGuid()));

        // Assert
        Assert.Null(acknowledged);
        Assert.Null(escalated);
    }

    [Fact]
    public async Task Handle_EscalateOverdueAlerts_EscalatesOnlyOverdueIssuedAlerts()
    {
        // Arrange
        var overdueCritical = await IssueAsync(AlertSeverity.Critical);
        var inTimeWarning = await IssueAsync(AlertSeverity.Warning);
        var acknowledged = await IssueAsync(AlertSeverity.Critical);
        await _commandService.Handle(new AcknowledgeAlertCommand(acknowledged.Id));
        _clock.Now = Start.AddSeconds(45);

        // Act
        var escalated = (await _commandService.Handle(new EscalateOverdueAlertsCommand())).ToList();

        // Assert
        Assert.Single(escalated);
        Assert.Equal(overdueCritical.Id, escalated[0].Id);
        Assert.Equal(AlertStatus.Issued, inTimeWarning.Status);
        Assert.Equal(AlertStatus.Acknowledged, acknowledged.Status);
    }

    [Fact]
    public async Task Handle_GetAlertsByStatus_ReturnsOnlyThatStatus()
    {
        // Arrange
        await IssueAsync(AlertSeverity.Warning);
        var escalated = await IssueAsync(AlertSeverity.Critical);
        await _commandService.Handle(new EscalateAlertCommand(escalated.Id));

        // Act
        var alerts = await _queryService.Handle(new GetAlertsByStatusQuery(AlertStatus.Escalated));

        // Assert
        Assert.Equal(escalated.Id, Assert.Single(alerts).Id);
    }
}
