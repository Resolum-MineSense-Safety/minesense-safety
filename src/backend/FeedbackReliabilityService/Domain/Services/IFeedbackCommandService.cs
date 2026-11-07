using FeedbackReliabilityService.Domain.Model.Aggregates;
using FeedbackReliabilityService.Domain.Model.Commands;

namespace FeedbackReliabilityService.Domain.Services;

public interface IFeedbackCommandService
{
    Task<AlertEvaluation> Handle(TrackAlertCommand command, CancellationToken cancellationToken);
    Task<AlertEvaluation> Handle(ReportFeedbackCommand command, CancellationToken cancellationToken);
    Task<AlertEvaluation> Handle(ReviewFeedbackCommand command, CancellationToken cancellationToken);
}
