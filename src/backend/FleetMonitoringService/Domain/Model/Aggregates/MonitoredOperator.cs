using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Domain.Model.Aggregates;

/// <summary>
/// Operator supervised by the control center (EP04): vehicle, fleet, shift,
/// location and current risk level.
/// </summary>
public class MonitoredOperator : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public string FullName { get; private set; }
    public string VehicleCode { get; private set; }
    public string Fleet { get; private set; }
    public Shift Shift { get; private set; }
    public string Location { get; private set; }
    public RiskLevel CurrentRiskLevel { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }

    public MonitoredOperator(RegisterMonitoredOperatorCommand command, DateTimeOffset registeredAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("A monitored operator must reference an operator.");

        OperatorId = command.OperatorId;
        FullName = Require(command.FullName, "Full name");
        VehicleCode = Require(command.VehicleCode, "Vehicle code");
        Fleet = Require(command.Fleet, "Fleet");
        Shift = command.Shift;
        Location = Require(command.Location, "Location");
        CurrentRiskLevel = RiskLevel.Normal;
        LastUpdatedAt = registeredAt;
    }

    public void UpdateRiskLevel(RiskLevel riskLevel, DateTimeOffset updatedAt)
    {
        CurrentRiskLevel = riskLevel;
        LastUpdatedAt = updatedAt;
    }

    public void ReassignVehicle(string vehicleCode, string location)
    {
        VehicleCode = Require(vehicleCode, "Vehicle code");
        Location = Require(location, "Location");
    }

    private static string Require(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? throw new DomainException($"{field} is required.")
            : value.Trim();
}
