using MonitoringService.Application.Internal.OutboundServices;

namespace MonitoringService.Tests.Fakes;

/// <summary>
/// Stands in for the Operations service: every context is valid unless the vehicle
/// code starts with <see cref="UnassignedPrefix"/>.
/// </summary>
public class FakeOperationalContextService : IOperationalContextService
{
    public const string UnassignedPrefix = "UNASSIGNED";
    public const string InvalidReason = "Operator is not assigned to this vehicle for the shift.";

    public Guid AssignmentId { get; } = Guid.NewGuid();
    public int Calls { get; private set; }

    public Task<OperationalContext> ValidateAsync(Guid operatorId, string vehicleCode, DateOnly date)
    {
        Calls++;
        return Task.FromResult(vehicleCode.StartsWith(UnassignedPrefix, StringComparison.OrdinalIgnoreCase)
            ? OperationalContext.Invalid(InvalidReason)
            : new OperationalContext(true, AssignmentId, null));
    }
}
