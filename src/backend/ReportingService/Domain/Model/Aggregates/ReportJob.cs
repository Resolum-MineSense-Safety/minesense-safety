using MineSenseSafety.Shared.Domain.Model;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;

namespace ReportingService.Domain.Model.Aggregates;

/// <summary>
/// Async safety report generation job (US22, TS-21).
/// Heavy PDF rendering is delegated to Lambda (ADR-008); this aggregate tracks the lifecycle.
/// Scaffold: shape only, generation arrives with Fase 2.
/// </summary>
public class ReportJob : AggregateRoot
{
    public DateTimeOffset From { get; private set; }
    public DateTimeOffset To { get; private set; }
    public string? SiteId { get; private set; }
    public string? FleetId { get; private set; }
    public string DefinitionCode { get; private set; } = string.Empty;
    public ReportFormat Format { get; private set; }
    public Guid RequestedBy { get; private set; }
    public ReportJobStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }

    public ReportJob(RequestReportGenerationCommand command, DateTimeOffset requestedAt)
    {
        if (command.From > command.To)
            throw new DomainException("The report start date must not be after the end date.");

        From = command.From;
        To = command.To;
        SiteId = command.SiteId;
        FleetId = command.FleetId;
        DefinitionCode = command.DefinitionCode;
        Format = command.Format;
        RequestedBy = command.RequestedBy;
        Status = ReportJobStatus.Requested;
        RequestedAt = requestedAt;
    }
}
