using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Infrastructure.Persistence.InMemory;

namespace AlertService.Tests.Infrastructure;

public class InMemoryAlertRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 6, 8, 0, 0, TimeSpan.Zero);
    private readonly InMemoryAlertRepository _repository = new();

    private static Alert IssuedAlert(Guid operatorId, int seconds) =>
        new(new IssueAlertCommand(operatorId, Guid.NewGuid(), AlertSeverity.Critical), Start.AddSeconds(seconds));

    [Fact]
    public async Task AddAndUpdate_StoreTheAlert()
    {
        // Arrange
        var alert = IssuedAlert(Guid.NewGuid(), 0);
        await _repository.AddAsync(alert);

        // Act
        alert.Acknowledge(Start.AddSeconds(5));
        await _repository.UpdateAsync(alert);

        // Assert
        Assert.Equal(AlertStatus.Acknowledged, (await _repository.FindByIdAsync(alert.Id))!.Status);
        Assert.Single(await _repository.ListAsync());
        Assert.Null(await _repository.FindByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task FindByOperatorIdAsync_ReturnsNewestFirst()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _repository.AddAsync(IssuedAlert(operatorId, 10));
        await _repository.AddAsync(IssuedAlert(operatorId, 50));
        await _repository.AddAsync(IssuedAlert(Guid.NewGuid(), 90));

        // Act
        var alerts = (await _repository.FindByOperatorIdAsync(operatorId)).ToList();

        // Assert
        Assert.Equal(2, alerts.Count);
        Assert.Equal(Start.AddSeconds(50), alerts[0].IssuedAt);
    }

    [Fact]
    public async Task FindByStatusAsync_ReturnsOldestFirst()
    {
        // Arrange
        var newer = IssuedAlert(Guid.NewGuid(), 40);
        var older = IssuedAlert(Guid.NewGuid(), 20);
        var escalated = IssuedAlert(Guid.NewGuid(), 0);
        escalated.Escalate();
        await _repository.AddAsync(newer);
        await _repository.AddAsync(older);
        await _repository.AddAsync(escalated);

        // Act
        var issued = (await _repository.FindByStatusAsync(AlertStatus.Issued)).ToList();

        // Assert
        Assert.Equal([older.Id, newer.Id], issued.Select(alert => alert.Id));
    }
}
