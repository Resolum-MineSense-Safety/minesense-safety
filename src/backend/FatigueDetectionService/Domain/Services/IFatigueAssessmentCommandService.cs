using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;

namespace FatigueDetectionService.Domain.Services;

public interface IFatigueAssessmentCommandService
{
    Task<FatigueAssessment> Handle(AssessFatigueCommand command);
}
