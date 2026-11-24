using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IncidentManagementService.Interfaces.REST.Resources;

/// <summary>Body of TS-17; a missing type, moment or result is answered with 400.</summary>
public record RecordIncidentActionResource(
    [Required] string ActionType,
    [Required] string Description,
    [Required] string Result,
    [property: JsonRequired] Guid PerformedBy,
    [property: JsonRequired] DateTimeOffset PerformedAt
);
