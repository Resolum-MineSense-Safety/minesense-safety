using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace AlertService.Interfaces.REST.Transform;

public static class IssueAlertCommandFromResourceAssembler
{
    public static IssueAlertCommand ToCommandFromResource(IssueAlertResource resource)
    {
        if (!Enum.TryParse<AlertSeverity>(resource.Severity, ignoreCase: true, out var severity))
            throw new DomainException($"Unknown alert severity '{resource.Severity}'.");

        return new IssueAlertCommand(resource.OperatorId, resource.AssessmentId, severity);
    }
}
