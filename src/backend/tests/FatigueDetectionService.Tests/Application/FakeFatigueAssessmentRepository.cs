using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Repositories;

namespace FatigueDetectionService.Tests.Application;

/// <summary>Repository double so the application layer is tested without Infrastructure.</summary>
internal class FakeFatigueAssessmentRepository : IFatigueAssessmentRepository
{
    public List<FatigueAssessment> Items { get; } = [];
    public int SessionQueries { get; private set; }

    public Task AddAsync(FatigueAssessment aggregate)
    {
        Items.Add(aggregate);
        return Task.CompletedTask;
    }

    public Task<FatigueAssessment?> FindByIdAsync(Guid id) =>
        Task.FromResult(Items.Find(item => item.Id == id));

    public Task<IEnumerable<FatigueAssessment>> ListAsync() =>
        Task.FromResult<IEnumerable<FatigueAssessment>>(Items);

    public Task UpdateAsync(FatigueAssessment aggregate) => Task.CompletedTask;

    public Task<IEnumerable<FatigueAssessment>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<FatigueAssessment>>(
            Items.Where(item => item.OperatorId == operatorId).OrderByDescending(item => item.AssessedAt).ToList());

    public Task<IEnumerable<FatigueAssessment>> FindByMonitoringSessionIdAsync(Guid monitoringSessionId)
    {
        SessionQueries++;
        return Task.FromResult<IEnumerable<FatigueAssessment>>(
            Items.Where(item => item.MonitoringSessionId == monitoringSessionId).ToList());
    }
}
