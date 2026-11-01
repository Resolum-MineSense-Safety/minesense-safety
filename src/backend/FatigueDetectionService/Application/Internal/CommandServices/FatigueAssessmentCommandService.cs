using FatigueDetectionService.Application.Internal.IntegrationEvents;
using FatigueDetectionService.Domain.Model.Aggregates;
using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Repositories;
using FatigueDetectionService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Domain.Model;

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

    /// <summary>
    /// Stores the reading as history and publishes nothing: FatigueAssessed means "this is the risk of
    /// the operator now" (Alerts warns, Fleet Monitoring sets the current risk), and neither is true
    /// for a past reading. The cabin already warned, and its warning arrives as its own event (ADR-004).
    /// </summary>
    public async Task<FatigueAssessment> Handle(RecordEdgeAssessmentCommand command)
    {
        if (await fatigueAssessmentRepository.FindByIdAsync(command.AssessmentId) is { } existing)
            return existing;

        var assessment = new FatigueAssessment(command, timeProvider.GetUtcNow());
        try
        {
            await fatigueAssessmentRepository.AddAsync(assessment);
        }
        catch (DomainConflictException)
        {
            // Another consumer stored the same reading first.
            return (await fatigueAssessmentRepository.FindByIdAsync(command.AssessmentId))!;
        }
        return assessment;
    }
}
