using AlertService.Domain.Model.Aggregates;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Repositories;

namespace AlertService.Tests.Application;

/// <summary>Repository double so the application layer is tested without Infrastructure.</summary>
internal class FakeAlertRepository : IAlertRepository
{
    public List<Alert> Items { get; } = [];
    public int Updates { get; private set; }

    public Task AddAsync(Alert aggregate)
    {
        Items.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<Alert?> FindByIdAsync(Guid id) =>
        Task.FromResult(Items.Find(item => item.Id == id));

    public Task<IEnumerable<Alert>> ListAsync() =>
        Task.FromResult<IEnumerable<Alert>>(Items);

    public Task UpdateAsync(Alert aggregate)
    {
        Updates++;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Alert>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<Alert>>(Items.Where(item => item.OperatorId == operatorId).ToList());

    public Task<IEnumerable<Alert>> FindByStatusAsync(AlertStatus status) =>
        Task.FromResult<IEnumerable<Alert>>(Items.Where(item => item.Status == status).ToList());
}

/// <summary>Clock the tests can move forward.</summary>
internal class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;
    public override DateTimeOffset GetUtcNow() => Now;
}
