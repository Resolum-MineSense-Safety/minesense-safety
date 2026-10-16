using System.Text.Json;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace EdgeSynchronizationService.Domain.Model.Aggregates;

public class SyncEvent : AggregateRoot
{
    public Guid EventId { get; private set; }
    public string VehicleId { get; private set; }
    public string EventType { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string Payload { get; private set; }
    public SyncEventStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTimeOffset? SynchronizedAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public string? LastError { get; private set; }

    public SyncEvent(EnqueueSyncEventCommand command)
    {
        if (command.EventId == Guid.Empty) throw new DomainException("An event id is required.");
        if (string.IsNullOrWhiteSpace(command.VehicleId)) throw new DomainException("A vehicle id is required.");
        if (string.IsNullOrWhiteSpace(command.EventType)) throw new DomainException("An event type is required.");
        if (command.OccurredAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw new DomainException("An event cannot be dated in the future.");
        try
        {
            using var document = JsonDocument.Parse(command.Payload);
        }
        catch (JsonException)
        {
            throw new DomainException("The event payload must be valid JSON.");
        }

        EventId = command.EventId;
        VehicleId = command.VehicleId.Trim();
        EventType = command.EventType.Trim();
        OccurredAt = command.OccurredAt;
        Payload = command.Payload;
        Status = SyncEventStatus.Pending;
    }

    public void MarkSynchronized(DateTimeOffset at)
    {
        if (Status == SyncEventStatus.Synchronized) return;
        Status = SyncEventStatus.Synchronized;
        SynchronizedAt = at;
        LastAttemptAt = at;
        LastError = null;
        Attempts++;
    }

    public void MarkFailed(DateTimeOffset at, string error)
    {
        if (string.IsNullOrWhiteSpace(error)) throw new DomainException("A synchronization error is required.");
        Status = SyncEventStatus.Failed;
        LastAttemptAt = at;
        LastError = error.Trim();
        Attempts++;
    }
}
