namespace DeviceManagementService.Interfaces.REST.Resources;

public record PreShiftCheckItemResource(string DeviceType, string? SerialNumber, string Result, string? RecommendedAction);
