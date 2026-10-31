using FatigueDetectionService.Application.Internal.IntegrationEvents;
using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace FatigueDetectionService.Application.Internal.CommandServices;

public class FatigueAssessmentCommandService(
    IFatigueAssessmentRepository fatigueAssessmentRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher eventPublisher) : IFatigueAssessmentCommandService
{
    public async Task<FatigueAssessment> Handle(AssessFatigueCommand command)
    {
        var assessment = new FatigueAssessment(command, timeProvider.GetUtcNow());
        await fatigueAssessmentRepository.AddAsync(assessment);

        // Keyed by operator so the risk updates of one operator are consumed in order.
        var integrationEvent = IntegrationEvent.Create(FatigueAssessedEvent.Type, "fatigue-detection-service",
            assessment.Id, FatigueAssessedEvent.FromAssessment(assessment), assessment.AssessedAt,
            correlationId: assessment.Id);
        await eventPublisher.PublishAsync(IntegrationEventTopics.FatigueAssessed, integrationEvent,
            assessment.OperatorId.ToString());
        return assessment;
    }
}
