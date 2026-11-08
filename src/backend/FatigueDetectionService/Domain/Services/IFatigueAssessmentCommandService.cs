using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;

namespace FatigueDetectionService.Domain.Services;

public interface IFatigueAssessmentCommandService
{
    Task<FatigueAssessment> Handle(AssessFatigueCommand command);

    /// <summary>Records a reading synchronized from the Edge; recording it again returns the same assessment.</summary>
    Task<FatigueAssessment> Handle(RecordEdgeAssessmentCommand command);
}
