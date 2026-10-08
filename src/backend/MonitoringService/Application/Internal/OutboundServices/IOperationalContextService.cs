namespace MonitoringService.Application.Internal.OutboundServices;

// Sprint 1 · T18 (Andreow Santiago): outbound port to validate the operational context (operator + vehicle + shift date) before monitoring starts.
/// <summary>
/// Outbound port towards the Operations bounded context.
/// </summary>
public interface IOperationalContextService
{
    Task<OperationalContext> ValidateAsync(Guid operatorId, string vehicleCode, DateOnly date);
}
