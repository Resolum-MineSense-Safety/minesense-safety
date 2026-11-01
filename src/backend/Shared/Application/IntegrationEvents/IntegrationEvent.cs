using System.Text.Json;

namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Envelope of every domain event exchanged through Kafka (ADR-004). The payload is plain JSON so
/// producer and consumer never share types: each bounded context reads only the fields it needs.
/// <see cref="EventId"/> lets consumers discard redeliveries and <see cref="CorrelationId"/> traces
/// one detection chain (assessment, alert, incident) across the services.
/// </summary>
public sealed record IntegrationEvent(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAt,
    int Version,
    string Source,
    Guid CorrelationId,
    string AggregateId,
    JsonElement Payload)
{
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IntegrationEvent Create<TPayload>(string type, string source, Guid aggregateId, TPayload payload,
        DateTimeOffset occurredAt, Guid? correlationId = null, int version = 1)
    {
        var eventId = Guid.NewGuid();
        return new IntegrationEvent(eventId, type, occurredAt, version, source,
            correlationId ?? IntegrationEventContext.CorrelationId ?? eventId, aggregateId.ToString(),
            JsonSerializer.SerializeToElement(payload, SerializerOptions));
    }

    public TPayload GetPayload<TPayload>() =>
        Payload.Deserialize<TPayload>(SerializerOptions)
        ?? throw new JsonException($"Event {EventId} of type {Type} has an empty payload.");

    public string Serialize() => JsonSerializer.Serialize(this, SerializerOptions);

    /// <summary>Reads an envelope; returns null when the message is not a valid MineSense event.</summary>
    public static IntegrationEvent? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var integrationEvent = JsonSerializer.Deserialize<IntegrationEvent>(json, SerializerOptions);
            return integrationEvent is null || integrationEvent.EventId == Guid.Empty ||
                   string.IsNullOrWhiteSpace(integrationEvent.Type)
                ? null
                : integrationEvent;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
