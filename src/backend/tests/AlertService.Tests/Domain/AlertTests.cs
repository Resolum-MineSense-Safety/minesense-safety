using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
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
    public void Escalate_AcknowledgedAlert_ThrowsDomainException()
    {
        // Arrange
        var alert = IssueCriticalAlert();
        alert.Acknowledge(IssuedAt.AddSeconds(4));

        // Act & Assert
        Assert.Throws<DomainException>(alert.Escalate);
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
