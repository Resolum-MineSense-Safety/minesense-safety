using MineSenseSafety.Shared.Domain.Model;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Domain.Model.Aggregates;

/// <summary>
/// Async compliance report request (US22, TS-21, ADR-008).
/// Heavy PDF generation is delegated to Lambda; this aggregate tracks the request lifecycle.
/// Scaffold: shape only, generation arrives with Fase 4.
/// </summary>
public class ComplianceReport : AggregateRoot
{
    public DateTimeOffset From { get; private set; }
    public DateTimeOffset To { get; private set; }
    public string? SiteId { get; private set; }
    public string? FleetId { get; private set; }
    public Guid RequestedBy { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }

    public ComplianceReport(RequestSafetyReportCommand command, DateTimeOffset requestedAt)
    {
        if (command.From > command.To)
            throw new DomainException("The report start date must not be after the end date.");

        From = command.From;
        To = command.To;
        SiteId = command.SiteId;
        FleetId = command.FleetId;
        RequestedBy = command.RequestedBy;
        Status = ReportStatus.Requested;
        RequestedAt = requestedAt;
    }
}
