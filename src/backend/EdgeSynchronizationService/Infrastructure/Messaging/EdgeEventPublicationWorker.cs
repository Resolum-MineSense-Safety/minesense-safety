using EdgeSynchronizationService.Application.Internal.CommandServices;

namespace EdgeSynchronizationService.Infrastructure.Messaging;

/// <summary>
/// Publishes pending batches in the background, so the answer to the cabin never waits for Kafka and
/// a broker outage only delays the events. Runs only when Kafka is configured.
/// </summary>
public sealed class EdgeEventPublicationWorker(
    EdgeEventPublicationService publicationService,
    ILogger<EdgeEventPublicationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        var delay = PollInterval;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await publicationService.PublishPendingAsync(stoppingToken);
                delay = PollInterval;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxBackoff.Ticks));
                logger.LogWarning(exception, "Edge batches stay pending; publishing again in {Delay}", delay);
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
