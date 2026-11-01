using System.Collections.Concurrent;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Infrastructure.Messaging;

/// <summary>
/// Processed event ids kept in memory, like the repositories of this slice. It keeps the most
/// recent <see cref="Capacity"/> ids; older redeliveries are still absorbed by the idempotent
/// business keys of each handler (assessment id, alert id...).
/// </summary>
public sealed class InMemoryProcessedEventStore : IProcessedEventStore
{
    public const int Capacity = 50_000;

    private readonly ConcurrentDictionary<Guid, byte> _processed = new();
    private readonly ConcurrentQueue<Guid> _order = new();

    public bool HasProcessed(Guid eventId) => _processed.ContainsKey(eventId);

    public void MarkProcessed(Guid eventId)
    {
        if (!_processed.TryAdd(eventId, 0)) return;
        _order.Enqueue(eventId);
        while (_order.Count > Capacity && _order.TryDequeue(out var oldest))
            _processed.TryRemove(oldest, out _);
    }
}
