using DeviceManagementService.Domain.Model.ValueObjects;
using DeviceManagementService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Domain.Model.Aggregates;

/// <summary>
/// Verification of the operator's devices before starting the shift (US15).
/// The protection level is derived from the items with <see cref="PreShiftCheckPolicy"/>.
/// </summary>
public class PreShiftCheck : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public string VehicleCode { get; private set; }
    public DateTimeOffset CheckedAt { get; private set; }
    public IReadOnlyList<PreShiftCheckItem> Items { get; private set; }
    public ProtectionLevel Protection { get; private set; }

    /// <summary>Monitoring can start only when no indispensable device is missing.</summary>
    public bool CanStartMonitoring => Protection != ProtectionLevel.NotAvailable;

    public PreShiftCheck(Guid operatorId, string vehicleCode, DateTimeOffset checkedAt, IEnumerable<PreShiftCheckItem> items)
    {
        if (operatorId == Guid.Empty)
            throw new DomainException("A pre-shift check must belong to an operator.");
        if (string.IsNullOrWhiteSpace(vehicleCode))
            throw new DomainException("A pre-shift check must indicate the vehicle.");

        OperatorId = operatorId;
        VehicleCode = vehicleCode.Trim();
        CheckedAt = checkedAt;
        Items = items.ToList().AsReadOnly();
        Protection = PreShiftCheckPolicy.DetermineProtection(Items);
    }
}
