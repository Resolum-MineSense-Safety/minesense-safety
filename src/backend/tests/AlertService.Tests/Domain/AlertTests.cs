using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace AlertService.Tests.Domain;

public class AlertTests
{
    private static readonly DateTimeOffset IssuedAt = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);

    private static Alert IssueCriticalAlert() =>
        new(new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Critical), IssuedAt);

    [Fact]
    public void Constructor_WithValidCommand_StartsAsIssued()
    {
        // Arrange
        var command = new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Warning);

        // Act
        var alert = new Alert(command, IssuedAt);

        // Assert
        Assert.Equal(AlertStatus.Issued, alert.Status);
        Assert.Equal(IssuedAt, alert.IssuedAt);
        Assert.Null(alert.AcknowledgedAt);
    }

    [Fact]
    public void Constructor_WithoutOperator_ThrowsDomainException()
    {
        // Arrange
        var command = new IssueAlertCommand(Guid.Empty, Guid.NewGuid(), AlertSeverity.Critical);

        // Act & Assert
        Assert.Throws<DomainException>(() => new Alert(command, IssuedAt));
    }

    [Fact]
    public void Acknowledge_IssuedAlert_RecordsAcknowledgementTime()
    {
        // Arrange
        var alert = IssueCriticalAlert();
        var acknowledgedAt = IssuedAt.AddSeconds(4);

        // Act
        alert.Acknowledge(acknowledgedAt);

        // Assert
        Assert.Equal(AlertStatus.Acknowledged, alert.Status);
        Assert.Equal(acknowledgedAt, alert.AcknowledgedAt);
    }

    [Fact]
    public void Acknowledge_EscalatedAlert_ThrowsDomainException()
    {
        // Arrange
        var alert = IssueCriticalAlert();
        alert.Escalate();

        // Act & Assert
        Assert.Throws<DomainException>(() => alert.Acknowledge(IssuedAt.AddMinutes(1)));
    }

    [Fact]
    public void Escalate_AcknowledgedAlert_ThrowsDomainException()
    {
        // Arrange
        var alert = IssueCriticalAlert();
        alert.Acknowledge(IssuedAt.AddSeconds(4));

        // Act & Assert
        Assert.Throws<DomainException>(alert.Escalate);
    }

    [Theory]
    [InlineData(AlertSeverity.Critical, 31, true)]
    [InlineData(AlertSeverity.Critical, 30, false)]
    [InlineData(AlertSeverity.Warning, 60, false)]
    [InlineData(AlertSeverity.Warning, 121, true)]
    public void IsAcknowledgementOverdue_IssuedAlert_DependsOnSeverityDeadline(
        AlertSeverity severity, int elapsedSeconds, bool expected)
    {
        // Arrange
        var alert = new Alert(new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), severity), IssuedAt);

        // Act
        var overdue = alert.IsAcknowledgementOverdue(IssuedAt.AddSeconds(elapsedSeconds));

        // Assert
        Assert.Equal(expected, overdue);
    }

    [Fact]
    public void IsAcknowledgementOverdue_AcknowledgedAlert_IsNeverOverdue()
    {
        // Arrange
        var alert = IssueCriticalAlert();
        alert.Acknowledge(IssuedAt.AddSeconds(4));

        // Act
        var overdue = alert.IsAcknowledgementOverdue(IssuedAt.AddHours(1));

        // Assert
        Assert.False(overdue);
    }

    [Fact]
    public void Messages_KeepTheirValues()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var acknowledge = new AcknowledgeAlertCommand(id);
        var escalate = new EscalateAlertCommand(id);
        var byId = new GetAlertByIdQuery(id);
        var byOperator = new GetAlertsByOperatorIdQuery(id);
        var byStatus = new GetAlertsByStatusQuery(AlertStatus.Escalated);

        // Assert
        Assert.Equal(id, acknowledge.AlertId);
        Assert.Equal(id, escalate.AlertId);
        Assert.Equal(id, byId.AlertId);
        Assert.Equal(id, byOperator.OperatorId);
        Assert.Equal(AlertStatus.Escalated, byStatus.Status);
        Assert.NotNull(new EscalateOverdueAlertsCommand());
    }

    [Fact]
    public void Escalate_UnacknowledgedAlert_ChangesStatusToEscalated()
    {
        // Arrange
        var alert = IssueCriticalAlert();

        // Act
        alert.Escalate();

        // Assert
        Assert.Equal(AlertStatus.Escalated, alert.Status);
    }
}
