using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;

namespace MonitoringService.Domain.Services;

public interface IMonitoringSessionCommandService
{
    Task<MonitoringSession> Handle(StartMonitoringCommand command);
    Task<MonitoringSession?> Handle(ReportSignalLostCommand command);
    Task<MonitoringSession?> Handle(ReportSignalRestoredCommand command);
    Task<MonitoringSession?> Handle(StopMonitoringCommand command);
}
