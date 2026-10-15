using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AnalyticsService.Interfaces.REST.Resources;

/// <summary>Measure applied to the operation; empty scope fields mean "whole operation".</summary>
public record RegisterPreventiveMeasureResource(
    [Required] string Name,
    [Required] string MeasureType,
    string? Description,
    [property: JsonRequired] DateTimeOffset AppliedAt,
    [property: JsonRequired] Guid RegisteredBy,
    string? Shift,
    string? MineSite,
    string? Location,
    string? Fleet
);
