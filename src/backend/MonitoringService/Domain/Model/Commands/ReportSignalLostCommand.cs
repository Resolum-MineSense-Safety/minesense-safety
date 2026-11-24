using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Model.Commands;

public record ReportSignalLostCommand(Guid SessionId, SignalType Signal);
