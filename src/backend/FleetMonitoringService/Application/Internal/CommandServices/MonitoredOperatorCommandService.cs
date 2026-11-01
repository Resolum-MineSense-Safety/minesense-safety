using FleetMonitoringService.Application.Internal.IntegrationEvents;
using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Repositories;
using FleetMonitoringService.Domain.Services;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Application.Internal.CommandServices;

public class MonitoredOperatorCommandService(
    IMonitoredOperatorRepository monitoredOperatorRepository,
    TimeProvider timeProvider,
    IIntegrationEventPublisher eventPublisher) : IMonitoredOperatorCommandService
{
    public async Task<MonitoredOperator> Handle(RegisterMonitoredOperatorCommand command)
    {
        if (await monitoredOperatorRepository.FindByOperatorIdAsync(command.OperatorId) is not null)
            throw new DomainException($"Operator {command.OperatorId} is already monitored.");

        var monitoredOperator = new MonitoredOperator(command, timeProvider.GetUtcNow());
        await monitoredOperatorRepository.AddAsync(monitoredOperator);
        await PublishUpdatedAsync(monitoredOperator);
        return monitoredOperator;
    }

    public async Task<MonitoredOperator?> Handle(UpdateOperatorRiskLevelCommand command)
    {
        var monitoredOperator = await monitoredOperatorRepository.FindByOperatorIdAsync(command.OperatorId);
        if (monitoredOperator is null) return null;

        monitoredOperator.UpdateRiskLevel(command.RiskLevel, timeProvider.GetUtcNow());
        await monitoredOperatorRepository.UpdateAsync(monitoredOperator);
        await PublishUpdatedAsync(monitoredOperator);
        return monitoredOperator;
    }

    public async Task<MonitoredOperator?> Handle(ReassignOperatorVehicleCommand command)
    {
        var monitoredOperator = await monitoredOperatorRepository.FindByOperatorIdAsync(command.OperatorId);
        if (monitoredOperator is null) return null;

        monitoredOperator.ReassignVehicle(command.VehicleCode, command.Location, timeProvider.GetUtcNow());
        await monitoredOperatorRepository.UpdateAsync(monitoredOperator);
        await PublishUpdatedAsync(monitoredOperator);
        return monitoredOperator;
    }

    /// <summary>Keyed by operator id, so the snapshots of one operator arrive in order.</summary>
    private Task PublishUpdatedAsync(MonitoredOperator monitoredOperator) =>
        eventPublisher.PublishAsync(IntegrationEventTopics.FleetOperatorUpdated,
            IntegrationEvent.Create(FleetOperatorUpdatedEvent.Type, "fleet-monitoring-service",
                monitoredOperator.OperatorId, FleetOperatorUpdatedEvent.FromOperator(monitoredOperator),
                monitoredOperator.LastUpdatedAt));
}
