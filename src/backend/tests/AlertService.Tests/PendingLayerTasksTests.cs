using AlertService.Application.Internal.CommandServices;
using AlertService.Application.Internal.QueryServices;
using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Infrastructure.Persistence.InMemory;

namespace AlertService.Tests;

/// <summary>
/// Guards the contracts left for the layer tasks (docs: reparto por capas).
/// When you implement your task, delete its test here and add the real tests in your layer folder.
/// </summary>
public class PendingLayerTasksTests
{
    private readonly InMemoryAlertRepository _repository = new();

    [Fact]
    public void A1_IsAcknowledgementOverdue_IsPending()
    {
        // Arrange
        var alert = new Alert(
            new IssueAlertCommand(Guid.NewGuid(), Guid.NewGuid(), AlertSeverity.Critical),
            DateTimeOffset.UtcNow);

        // Act
        var exception = Assert.Throws<NotImplementedException>(
            () => alert.IsAcknowledgementOverdue(DateTimeOffset.UtcNow));

        // Assert
        Assert.StartsWith("A1:", exception.Message);
    }

    [Fact]
    public async Task A2_EscalateOverdueAlerts_IsPending()
    {
        // Arrange
        var service = new AlertCommandService(_repository, TimeProvider.System);

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(
            () => service.Handle(new EscalateOverdueAlertsCommand()));

        // Assert
        Assert.StartsWith("A2:", exception.Message);
    }

    [Fact]
    public async Task A2_QueryByStatus_IsPending()
    {
        // Arrange
        var service = new AlertQueryService(_repository);

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(
            () => service.Handle(new GetAlertsByStatusQuery(AlertStatus.Issued)));

        // Assert
        Assert.StartsWith("A2:", exception.Message);
    }

    [Fact]
    public async Task A3_FindByStatus_IsPending()
    {
        // Arrange
        var status = AlertStatus.Issued;

        // Act
        var exception = await Assert.ThrowsAsync<NotImplementedException>(
            () => _repository.FindByStatusAsync(status));

        // Assert
        Assert.StartsWith("A3:", exception.Message);
    }
}
