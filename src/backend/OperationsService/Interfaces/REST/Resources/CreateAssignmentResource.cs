using System.Text.Json.Serialization;

namespace OperationsService.Interfaces.REST.Resources;

/// <summary>
/// Assignment request. TS-08 clients may send <c>startsAt</c>/<c>endsAt</c> (ISO 8601) instead of
/// <c>validFrom</c>/<c>validTo</c>; only the date part is kept because assignments are per shift day.
/// </summary>
public record CreateAssignmentResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] Guid VehicleId,
    string? Shift,
    [property: JsonRequired] DateOnly ValidFrom,
    DateOnly? ValidTo,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null
);