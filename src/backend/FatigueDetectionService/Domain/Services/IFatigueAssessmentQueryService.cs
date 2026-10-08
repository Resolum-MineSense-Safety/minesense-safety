using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Queries;

namespace FatigueDetectionService.Domain.Services;

public interface IFatigueAssessmentQueryService
{
    Task<FatigueAssessment?> Handle(GetFatigueAssessmentByIdQuery query);
    Task<IEnumerable<FatigueAssessment>> Handle(GetFatigueAssessmentsByOperatorIdQuery query);
    Task<FatigueAssessment?> Handle(GetLatestFatigueAssessmentByOperatorIdQuery query);
    Task<IEnumerable<FatigueAssessment>> Handle(GetFatigueAssessmentsByMonitoringSessionIdQuery query);
}
