using IncidentManagementService.Domain.Model.ValueObjects;

namespace IncidentManagementService.Domain.Model.Queries;

/// <summary>Incident tray with optional filters; null means "any".</summary>
public record GetIncidentsQuery(IncidentStatus? Status, Guid? OperatorId);
