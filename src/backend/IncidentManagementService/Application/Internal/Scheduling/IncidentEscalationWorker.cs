using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Services;

namespace IncidentManagementService.Application.Internal.Scheduling;

/// <summary>
/// Timer of US11: periodically escalates the critical incidents that are still
/// unattended, so the escalation does not depend on someone calling the API.
/// The interval comes from <c>IncidentEscalation:IntervalSeconds</c> (15 s by default).
/// </summary>
public class IncidentEscalationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<IncidentEscalationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var seconds = configuration.GetValue("IncidentEscalation:IntervalSeconds", 15);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope = scopeFactory.CreateScope();
            var commandService = scope.ServiceProvider.GetRequiredService<IIncidentCommandService>();
            var escalated = await commandService.Handle(new EscalateOverdueIncidentsCommand());

            foreach (var incident in escalated)
                logger.LogWarning("Incident {IncidentId} escalated to level {Level}",
                    incident.Id, incident.EscalationLevel);
        }
    }
}
