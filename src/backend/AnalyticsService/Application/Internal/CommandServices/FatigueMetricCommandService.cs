using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;

namespace AnalyticsService.Application.Internal.CommandServices;

public class FatigueMetricCommandService(IFatigueMetricRepository fatigueMetricRepository)
    : IFatigueMetricCommandService
{
    /// <summary>Records the observation once per alert, so resent events do not inflate the trends.</summary>
    public async Task<FatigueMetric> Handle(RecordFatigueMetricCommand command)
    {
        var existing = await fatigueMetricRepository.FindByAlertIdAsync(command.AlertId);
        if (existing is not null) return existing;

        var metric = new FatigueMetric(command);
        await fatigueMetricRepository.AddAsync(metric);
        return metric;
    }
}
