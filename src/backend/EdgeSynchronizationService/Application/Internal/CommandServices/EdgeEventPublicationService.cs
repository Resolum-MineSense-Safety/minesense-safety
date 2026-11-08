using EdgeSynchronizationService.Application.Internal.IntegrationEvents;
using EdgeSynchronizationService.Domain.Repositories;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace EdgeSynchronizationService.Application.Internal.CommandServices;

/// <summary>
/// Sends the stored batches that are not in Kafka yet, oldest first. A batch stops being pending only
/// after the broker confirmed all its events. The first failure stops the pass, so a later batch never
/// overtakes an earlier one; the failed batch is sent again whole with the same event ids.
/// </summary>
public sealed class EdgeEventPublicationService(
    ISynchronizationBatchRepository repository,
    IConfirmedIntegrationEventPublisher publisher,
    TimeProvider timeProvider)
{
    private const int PageSize = 20;

    /// <summary>Returns how many batches were published.</summary>
    public async Task<int> PublishPendingAsync(CancellationToken cancellationToken)
    {
        var published = 0;
        while (true)
        {
            var pending = await repository.FindPendingPublicationAsync(PageSize);
            if (pending.Count == 0) return published;

            foreach (var batch in pending)
            {
                foreach (var message in EdgeEventMessages.From(batch))
                    await publisher.PublishConfirmedAsync(message.Topic, message.Event, message.PartitionKey,
                        cancellationToken);

                batch.MarkPublished(timeProvider.GetUtcNow());
                await repository.UpdateAsync(batch);
                published++;
            }
        }
    }
}
