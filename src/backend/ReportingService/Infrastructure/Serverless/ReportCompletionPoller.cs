using ReportingService.Application.Internal.Serverless;

namespace ReportingService.Infrastructure.Serverless;

/// <summary>Periodically checks the jobs delegated to the Lambda. Registered only with the serverless flow.</summary>
public class ReportCompletionPoller(
    IServiceScopeFactory scopeFactory,
    ServerlessReportingOptions options,
    ILogger<ReportCompletionPoller> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<ReportCompletionService>()
                        .CheckPendingJobsAsync(stoppingToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogError(exception, "The pending report jobs could not be checked.");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The service is stopping.
        }
    }
}
