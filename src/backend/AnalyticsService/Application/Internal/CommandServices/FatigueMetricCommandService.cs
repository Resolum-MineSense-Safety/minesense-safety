using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Repositories;
using AnalyticsService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Application.Internal.CommandServices;

public class FatigueMetricCommandService(IFatigueMetricRepository fatigueMetricRepository)
    : IFatigueMetricCommandService
{
    /// <summary>
    /// Records the observation once per alert, so resent events do not inflate the trends. The
    /// AlertIssued consumer and a direct call for the same alert can race; the loser gets the
    /// unique-key conflict and returns the observation the winner stored instead of a 409.
    /// </summary>
    public async Task<FatigueMetric> Handle(RecordFatigueMetricCommand command)
    {
        var existing = await fatigueMetricRepository.FindByAlertIdAsync(command.AlertId);
        if (existing is not null) return existing;

        var metric = new FatigueMetric(command);
        try
        {
            await fatigueMetricRepository.AddAsync(metric);
        }
        catch (DomainConflictException)
        {
            var stored = await fatigueMetricRepository.FindByAlertIdAsync(command.AlertId);
            if (stored is null) throw;
            return stored;
        }

        return metric;
    }
}
