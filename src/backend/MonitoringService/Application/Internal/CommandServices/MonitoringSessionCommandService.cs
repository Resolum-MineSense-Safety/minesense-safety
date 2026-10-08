using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Repositories;
using MonitoringService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Application.Internal.CommandServices;

// Sprint 1 · T04 (Farid Coronel): start of monitoring (US01) — rejects a second open session, validates the operational context (T18) and records the available signals.
// Sprint 1 · T05 (Ian Santisteban): the signal loss/restoration handlers are what the Edge gateway triggers (see MonitoringSessionsController).
public class MonitoringSessionCommandService(
    IMonitoringSessionRepository sessionRepository,
    IOperationalContextService operationalContextService,
    TimeProvider timeProvider) : IMonitoringSessionCommandService
{
    public async Task<MonitoringSession> Handle(StartMonitoringCommand command)
    {
        var openSession = await sessionRepository.FindOpenByOperatorIdAsync(command.OperatorId);
        if (openSession is not null)
            throw new DomainException($"The operator already has an open monitoring session ({openSession.Id}).");

        var now = timeProvider.GetUtcNow();
        // Sprint 1 · T18 (Andreow Santiago): monitoring only starts within a valid operational context.
        var context = await operationalContextService.ValidateAsync(
            command.OperatorId, command.VehicleCode, DateOnly.FromDateTime(now.UtcDateTime));
        if (!context.Valid)
            throw new DomainException(context.Reason ?? "The operational context is not valid.");
        if (context.AssignmentId is not { } assignmentId)
            throw new DomainException("The operational context did not provide an operator assignment.");

        var session = new MonitoringSession(command, assignmentId, now);
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
