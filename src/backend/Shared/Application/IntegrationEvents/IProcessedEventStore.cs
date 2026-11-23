namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>Remembers which event ids a service already handled, so redeliveries are ignored.</summary>
public interface IProcessedEventStore
{
    bool HasProcessed(Guid eventId);

    void MarkProcessed(Guid eventId);
}
