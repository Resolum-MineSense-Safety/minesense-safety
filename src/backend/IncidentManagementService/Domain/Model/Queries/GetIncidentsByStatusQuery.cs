using IncidentManagementService.Domain.Model.ValueObjects;

namespace IncidentManagementService.Domain.Model.Queries;

/// <summary>Incidents in the given status (all when null), oldest first.</summary>
public record GetIncidentsByStatusQuery(IncidentStatus? Status);
