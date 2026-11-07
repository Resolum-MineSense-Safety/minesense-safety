using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnalyticsService.Interfaces.REST.Resources;

/// <summary>Fatigue alert observed in the operation, with its analytical dimensions.</summary>
public record RecordFatigueEventResource(
    [property: JsonRequired] Guid OperatorId,
    [property: JsonRequired] Guid AlertId,
    string Severity,
    [property: JsonRequired] DateTimeOffset OccurredAt,
    [Required] string Shift,
    [Required] string MineSite,
    [Required] string Location,
    [Required] string Fleet
);
