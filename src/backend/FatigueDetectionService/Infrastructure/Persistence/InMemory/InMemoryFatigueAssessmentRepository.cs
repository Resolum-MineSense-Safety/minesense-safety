using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.InMemory;

namespace FatigueDetectionService.Infrastructure.Persistence.InMemory;

public class InMemoryFatigueAssessmentRepository
    : InMemoryRepository<FatigueAssessment>, IFatigueAssessmentRepository
{
    public Task<IEnumerable<FatigueAssessment>> FindByOperatorIdAsync(Guid operatorId) =>
        Task.FromResult<IEnumerable<FatigueAssessment>>(
            Store.Values.Where(assessment => assessment.OperatorId == operatorId)
                .OrderByDescending(assessment => assessment.AssessedAt)
                .ToList());

    // F3 (Infrastructure): filter by session and order by AssessedAt ascending.
    public Task<IEnumerable<FatigueAssessment>> FindByMonitoringSessionIdAsync(Guid monitoringSessionId) =>
        throw new NotImplementedException("F3: pending implementation.");
}
