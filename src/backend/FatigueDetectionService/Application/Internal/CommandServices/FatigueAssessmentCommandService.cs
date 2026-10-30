using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;

namespace FatigueDetectionService.Application.Internal.CommandServices;

public class FatigueAssessmentCommandService(
    IFatigueAssessmentRepository fatigueAssessmentRepository,
    TimeProvider timeProvider) : IFatigueAssessmentCommandService
{
    public async Task<FatigueAssessment> Handle(AssessFatigueCommand command)
    {
        var assessment = new FatigueAssessment(command, timeProvider.GetUtcNow());
        await fatigueAssessmentRepository.AddAsync(assessment);
        return assessment;
    }
}
