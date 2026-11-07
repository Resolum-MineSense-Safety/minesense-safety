using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;

namespace AnalyticsService.Application.Internal.CommandServices;

public class PreventiveMeasureCommandService(IPreventiveMeasureRepository preventiveMeasureRepository)
    : IPreventiveMeasureCommandService
{
    public async Task<PreventiveMeasure> Handle(RegisterPreventiveMeasureCommand command)
    {
        var measure = new PreventiveMeasure(command);
        await preventiveMeasureRepository.AddAsync(measure);
        return measure;
    }
}
