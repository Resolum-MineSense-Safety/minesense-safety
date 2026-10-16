using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;
using EdgeSynchronizationService.Infrastructure.Messaging;
using MineSenseSafety.Shared.Domain.Model;

namespace EdgeSynchronizationService.Application.Internal.CommandServices;

public class SynchronizationCommandService(
    IOutboxEventRepository repository,
    IMqttPublisher publisher,
    TimeProvider timeProvider) : ISynchronizationCommandService
{
    public async Task<(SyncEvent Event, bool AlreadyExists)> EnqueueAsync(EnqueueSyncEventCommand command)
    {
        var existing = await repository.FindByEventIdAsync(command.EventId);
        if (existing is not null) return (existing, true);
        var syncEvent = new SyncEvent(command);
        await repository.AddAsync(syncEvent);
        return (syncEvent, false);
    }

    public async Task<IEnumerable<SyncEvent>> SynchronizeAsync(SynchronizePendingEventsCommand command)
    {
        if (command.BatchSize is < 1 or > 500)
            throw new DomainException("Batch size must be between 1 and 500.");

        var events = await repository.FindPendingAsync(command.VehicleId, command.BatchSize);
        foreach (var syncEvent in events)
        {
            try
            {
                await publisher.PublishAsync(syncEvent);
                syncEvent.MarkSynchronized(timeProvider.GetUtcNow());
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                syncEvent.MarkFailed(timeProvider.GetUtcNow(), exception.Message);
            }
            await repository.UpdateAsync(syncEvent);
        }
        return events;
    }
}
