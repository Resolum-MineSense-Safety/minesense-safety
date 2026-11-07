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

    public async Task<IEnumerable<FatigueAssessment>> Handle(GetFatigueAssessmentsByMonitoringSessionIdQuery query)
    {
        if (query.MonitoringSessionId == Guid.Empty) return [];
        return await fatigueAssessmentRepository.FindByMonitoringSessionIdAsync(query.MonitoringSessionId);
    }
}
