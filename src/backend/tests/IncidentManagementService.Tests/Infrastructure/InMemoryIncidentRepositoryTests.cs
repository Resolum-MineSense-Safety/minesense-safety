using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Infrastructure.Persistence.InMemory;

namespace IncidentManagementService.Tests.Infrastructure;

public class InMemoryIncidentRepositoryTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 15, 8, 0, 0, TimeSpan.Zero);
    private readonly InMemoryIncidentRepository _repository = new();

    private async Task<Incident> AddAsync(Guid operatorId, DateTimeOffset openedAt, Guid? alertId = null)
    {
        var incident = new Incident(
            new OpenIncidentCommand(operatorId, alertId ?? Guid.NewGuid(), IncidentSeverity.Critical), openedAt);
        await _repository.AddAsync(incident);
        return incident;
    }

    [Fact]
    public async Task FindAsync_WithoutFilters_ReturnsAllOldestFirst()
    {
        // Arrange
        var newer = await AddAsync(Guid.NewGuid(), Start.AddMinutes(5));
        var older = await AddAsync(Guid.NewGuid(), Start);

        // Act
        var incidents = (await _repository.FindAsync(null, null)).ToList();

        // Assert
        Assert.Equal([older, newer], incidents);
    }

    [Fact]
    public async Task FindAsync_WithFilters_ReturnsMatchingIncidents()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var assigned = await AddAsync(operatorId, Start);
        var pending = await AddAsync(operatorId, Start.AddMinutes(1));
        await AddAsync(Guid.NewGuid(), Start.AddMinutes(2));
        assigned.Assign(Guid.NewGuid(), Start.AddMinutes(3));
        await _repository.UpdateAsync(assigned);

        // Act
        var result = await _repository.FindAsync(IncidentStatus.Pending, operatorId);

        // Assert
        Assert.Same(pending, Assert.Single(result));
    }

    [Fact]
    public async Task FindByAlertIdAsync_ReturnsIncidentOfThatAlert()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var incident = await AddAsync(Guid.NewGuid(), Start, alertId);
        await AddAsync(Guid.NewGuid(), Start);

        // Act & Assert
        Assert.Same(incident, await _repository.FindByAlertIdAsync(alertId));
        Assert.Null(await _repository.FindByAlertIdAsync(Guid.NewGuid()));
    }
}
