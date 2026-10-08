using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Queries;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;

namespace FatigueDetectionService.Application.Internal.QueryServices;

public class FatigueAssessmentQueryService(IFatigueAssessmentRepository fatigueAssessmentRepository)
    : IFatigueAssessmentQueryService
{
    public Task<FatigueAssessment?> Handle(GetFatigueAssessmentByIdQuery query) =>
        fatigueAssessmentRepository.FindByIdAsync(query.FatigueAssessmentId);

    public Task<IEnumerable<FatigueAssessment>> Handle(GetFatigueAssessmentsByOperatorIdQuery query) =>
        fatigueAssessmentRepository.FindByOperatorIdAsync(query.OperatorId);

    public async Task<FatigueAssessment?> Handle(GetLatestFatigueAssessmentByOperatorIdQuery query)
    {
        var assessments = await fatigueAssessmentRepository.FindByOperatorIdAsync(query.OperatorId);
        return assessments.FirstOrDefault();
    }

    // F2 (Application): return the assessments of the session; empty list when the id is Guid.Empty.
    public Task<IEnumerable<FatigueAssessment>> Handle(GetFatigueAssessmentsByMonitoringSessionIdQuery query) =>
        throw new NotImplementedException("F2: pending implementation.");
}
