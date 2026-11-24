namespace ReportingService.Domain.Model.ValueObjects;

/// <summary>Lifecycle of an async report generation job (US22, TS-21, ADR-008).</summary>
public enum ReportJobStatus
{
    Requested,
    Rendering,
    Ready,
    Empty,
    Failed
}
