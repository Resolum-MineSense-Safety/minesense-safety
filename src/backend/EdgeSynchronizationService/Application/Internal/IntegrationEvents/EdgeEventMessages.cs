using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Json;
using EdgeSynchronizationService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace EdgeSynchronizationService.Application.Internal.IntegrationEvents;

/// <summary>
/// Payload of an event synchronized from a cabin. <see cref="Data"/> is the payload the cabin wrote,
/// untouched: only the contexts that consume it know its fields. <see cref="CapturedAt"/> is the
/// moment of that event, not when the batch was sealed or received.
/// </summary>
public sealed record EdgeEventSynchronized(
    Guid EdgeEventId,
    Guid BatchId,
    Guid EdgeNodeId,
    Guid DeviceId,
    long SequenceNumber,
    int Position,
    DateTimeOffset CapturedAt,
    DateTimeOffset ReceivedAt,
    JsonElement Data);

public sealed record EdgeEventMessage(string Topic, IntegrationEvent Event, string PartitionKey);

/// <summary>Turns the contents of a batch into integration events, in capture order.</summary>
public static class EdgeEventMessages
{
    public const string Source = "edge-synchronization-service";
    public const string FatigueReadingEventType = "FatigueReadingClassified";
    public const string CabinWarningEventType = "CabinWarningIssued";
    public const string FatigueReadingSynchronizedType = "FatigueReadingSynchronized";
    public const string CabinWarningSynchronizedType = "CabinWarningSynchronized";

    /// <summary>
    /// Events of other types, or without a JSON object as payload, stay stored in the batch and are not
    /// published. They still count for the position, so the ids of the others never depend on them.
    /// Keyed by device: the events of one gateway stay in order within each topic, across batches.
    /// </summary>
    public static IEnumerable<EdgeEventMessage> From(SynchronizationBatch batch)
    {
        var position = -1;
        foreach (var item in batch.Events)
        {
            position++;
            var (topic, type) = item.EventType switch
            {
                FatigueReadingEventType => (IntegrationEventTopics.EdgeFatigueReadings, FatigueReadingSynchronizedType),
                CabinWarningEventType => (IntegrationEventTopics.EdgeCabinWarnings, CabinWarningSynchronizedType),
                _ => (null, null)
            };
            if (topic is null || type is null || item.Payload.ValueKind != JsonValueKind.Object) continue;

            var payload = new EdgeEventSynchronized(item.EventId, batch.BatchId, batch.EdgeNodeId, batch.DeviceId,
                batch.SequenceNumber, position, item.OccurredAt, batch.ReceivedAt, item.Payload);
            yield return new EdgeEventMessage(topic,
                IntegrationEvent.Create(type, Source, batch.BatchId, payload, item.OccurredAt,
                    correlationId: batch.BatchId, eventId: EventId(batch.BatchId, position)),
                batch.DeviceId.ToString());
        }
    }

    /// <summary>
    /// Same batch and position, same id, so publishing a batch again after a failure or a restart is
    /// discarded by the consumers. A name-based UUID (RFC 9562 version 8) over SHA-256.
    /// </summary>
    public static Guid EventId(Guid batchId, int position)
    {
        Span<byte> input = stackalloc byte[20];
        batchId.TryWriteBytes(input);
        BinaryPrimitives.WriteInt32BigEndian(input[16..], position);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(input, hash);
        var bytes = hash[..16];
        bytes[7] = (byte)((bytes[7] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
