using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EdgeSynchronizationService.Interfaces.REST.Resources;

public record EnqueueSyncEventResource(
    [property: JsonRequired] Guid EventId,
    [property: Required] string VehicleId,
    [property: Required] string EventType,
    [property: JsonRequired] DateTimeOffset OccurredAt,
    [property: JsonRequired] JsonElement Payload);
