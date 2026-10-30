using MineSenseSafety.Shared.Domain.Model;
using ReportingService.Domain.Model.Commands;

namespace ReportingService.Domain.Model.Aggregates;

/// <summary>
/// Versioned report indicator definition (US22 "definir indicadores").
/// New versions supersede; definitions are never edited destructively.
/// Scaffold: shape only, rules arrive with Fase 1.
/// </summary>
public class ReportDefinition : AggregateRoot
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Version { get; private set; } = string.Empty;
    public List<string> DataSources { get; private set; } = new();
    public bool IsActive { get; private set; }

    public ReportDefinition(DefineReportIndicatorCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            throw new DomainException("A report definition must have a code.");

        Code = command.Code;
        Name = command.Name;
        Version = command.Version;
        DataSources = command.DataSources.ToList();
        IsActive = true;
    }
}
