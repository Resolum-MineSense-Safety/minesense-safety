using IncidentManagementService.Domain.Model.Aggregates;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Repositories;

namespace IncidentManagementService.Tests.Application;

/// <summary>Repository double so the application layer is tested without Infrastructure.</summary>
internal class FakeIncidentRepository : IIncidentRepository
{
    public List<Incident> Items { get; } = [];
    public int Updates { get; private set; }

    public Task AddAsync(Incident aggregate)
    {
        Items.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<Incident?> FindByIdAsync(Guid id) =>
        Task.FromResult(Items.Find(item => item.Id == id));

    public Task<IEnumerable<Incident>> ListAsync() =>
        Task.FromResult<IEnumerable<Incident>>(Items);

    public Task UpdateAsync(Incident aggregate)
    {
        Updates++;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Incident>> FindAsync(IncidentStatus? status, Guid? operatorId) =>
        Task.FromResult<IEnumerable<Incident>>(Items
            .Where(item => status is null || item.Status == status)
            .Where(item => operatorId is null || item.OperatorId == operatorId)
            .ToList());

    public Task<Incident?> FindByAlertIdAsync(Guid alertId) =>
        Task.FromResult(Items.Find(item => item.AlertId == alertId));
}

/// <summary>Clock the tests can move forward.</summary>
internal class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
