using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Model.Commands;

public record ReportSignalRestoredCommand(Guid SessionId, SignalType Signal);
