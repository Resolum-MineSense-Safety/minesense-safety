using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;

namespace AnalyticsService.Domain.Services;

public interface IPreventiveMeasureCommandService
{
    Task<PreventiveMeasure> Handle(RegisterPreventiveMeasureCommand command);
}
