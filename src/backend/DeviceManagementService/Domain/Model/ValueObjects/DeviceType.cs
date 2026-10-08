namespace DeviceManagementService.Domain.Model.ValueObjects;

/// <summary>Kind of monitoring or warning device installed in the vehicle or worn by the operator.</summary>
public enum DeviceType
{
    Camera,
    Wearable,
    VibrationAlarm,
    EdgeGateway
}
