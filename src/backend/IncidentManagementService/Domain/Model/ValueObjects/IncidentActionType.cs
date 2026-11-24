namespace IncidentManagementService.Domain.Model.ValueObjects;

/// <summary>Responses a supervisor can adopt to protect the operator (US10).</summary>
public enum IncidentActionType
{
    OperatorContact,
    PreventivePause,
    OperatorRelief,
    Other
}
