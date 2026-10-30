using MineSenseSafety.Shared.Domain.Model;
using ReportingService.Domain.Model.Commands;
using ReportingService.Domain.Model.ValueObjects;

namespace ReportingService.Domain.Model.Aggregates;

/// <summary>
/// Async safety report generation job (US22, TS-21).
/// Lifecycle: Requested -> Rendering -> Ready | Failed. Heavy PDF rendering is delegated to
/// Lambda in the cloud (ADR-008); locally the job is rendered in process.
/// </summary>
public class ReportJob : AggregateRoot
{
    public DateTimeOffset From { get; private set; }
    public DateTimeOffset To { get; private set; }
    public string? SiteId { get; private set; }
    public string? FleetId { get; private set; }
    public string DefinitionCode { get; private set; } = string.Empty;
    public string DefinitionVersion { get; private set; } = string.Empty;
    public ReportFormat Format { get; private set; }
    public Guid RequestedBy { get; private set; }
    public ReportJobStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public ReportJob(RequestReportGenerationCommand command, string definitionVersion, DateTimeOffset requestedAt)
    {
        if (command.From > command.To)
            throw new DomainException("The report start date must not be after the end date.");
        if (command.RequestedBy == Guid.Empty)
            throw new DomainException("A report job must record who requested it.");

        From = command.From;
        To = command.To;
        SiteId = command.SiteId;
        FleetId = command.FleetId;
        DefinitionCode = command.DefinitionCode;
        DefinitionVersion = definitionVersion;
        Format = command.Format;
        RequestedBy = command.RequestedBy;
        Status = ReportJobStatus.Requested;
        RequestedAt = requestedAt;
    }

    public void StartRendering()
    {
        if (Status != ReportJobStatus.Requested)
            throw new DomainException($"A report job in status {Status} cannot start rendering.");
        Status = ReportJobStatus.Rendering;
    }

    public void MarkReady(DateTimeOffset completedAt)
    {
        EnsureRendering();
        Status = ReportJobStatus.Ready;
        CompletedAt = completedAt;
    }

    public void MarkFailed(string reason, DateTimeOffset completedAt)
    {
        EnsureRendering();
        Status = ReportJobStatus.Failed;
        FailureReason = reason;
        CompletedAt = completedAt;
    }

    private void EnsureRendering()
    {
        if (Status != ReportJobStatus.Rendering)
            throw new DomainException($"A report job in status {Status} is not rendering.");
    }
}
