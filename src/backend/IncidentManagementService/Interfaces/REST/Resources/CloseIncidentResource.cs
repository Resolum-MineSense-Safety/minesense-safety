using System.ComponentModel.DataAnnotations;

namespace IncidentManagementService.Interfaces.REST.Resources;

/// <summary>Body of TS-18; closedAt defaults to the server time.</summary>
public record CloseIncidentResource([Required] string Resolution, DateTimeOffset? ClosedAt);
