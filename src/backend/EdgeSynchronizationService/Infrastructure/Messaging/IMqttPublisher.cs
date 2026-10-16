using EdgeSynchronizationService.Domain.Model.Aggregates;

namespace EdgeSynchronizationService.Infrastructure.Messaging;

public interface IMqttPublisher
{
    Task PublishAsync(SyncEvent syncEvent);
}
