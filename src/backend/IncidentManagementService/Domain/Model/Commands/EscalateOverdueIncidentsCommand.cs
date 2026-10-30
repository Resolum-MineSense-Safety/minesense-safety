namespace IncidentManagementService.Domain.Model.Commands;

/// <summary>Raises one level every critical incident that is still unattended past its deadline.</summary>
public record EscalateOverdueIncidentsCommand;
