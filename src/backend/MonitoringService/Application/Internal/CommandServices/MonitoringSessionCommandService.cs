using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Repositories;
using MonitoringService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Application.Internal.CommandServices;

// Sprint 1 · T04 (Farid Coronel): start of monitoring (US01) — rejects a second open session, requires the operator's devices to be available (Device Management) and records the available signals.
// Sprint 1 · T05 (Ian Santisteban): the signal loss/restoration handlers are what the Edge gateway triggers (see MonitoringSessionsController).
public class MonitoringSessionCommandService(
    IMonitoringSessionRepository sessionRepository,
    IDeviceAvailabilityService deviceAvailabilityService,
    TimeProvider timeProvider) : IMonitoringSessionCommandService
{
    public async Task<MonitoringSession> Handle(StartMonitoringCommand command)
    {
        var openSession = await sessionRepository.FindOpenByOperatorIdAsync(command.OperatorId);
        if (openSession is not null)
            throw new DomainException($"The operator already has an open monitoring session ({openSession.Id}).");

        var availability = await deviceAvailabilityService.CheckAsync(command.OperatorId);
        if (!availability.CanStartMonitoring)
            throw new DomainException(availability.Reason ?? "The operator's devices are not available.");

        var session = new MonitoringSession(command, timeProvider.GetUtcNow());
        await sessionRepository.AddAsync(session);
        return session;
    }

    public async Task<MonitoringSession?> Handle(ReportSignalLostCommand command)
    {
        var session = await sessionRepository.FindByIdAsync(command.SessionId);
        if (session is null) return null;

        session.ReportSignalLost(command.Signal, timeProvider.GetUtcNow());
        await sessionRepository.UpdateAsync(session);
        return session;
    }

    public async Task<MonitoringSession?> Handle(ReportSignalRestoredCommand command)
    {
        var session = await sessionRepository.FindByIdAsync(command.SessionId);
        if (session is null) return null;

        session.ReportSignalRestored(command.Signal, timeProvider.GetUtcNow());
        await sessionRepository.UpdateAsync(session);
        return session;
    }

    public async Task<MonitoringSession?> Handle(StopMonitoringCommand command)
    {
        var session = await sessionRepository.FindByIdAsync(command.SessionId);
        if (session is null) return null;

        session.Stop(timeProvider.GetUtcNow());
        await sessionRepository.UpdateAsync(session);
        return session;
    }
}
