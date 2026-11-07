using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Domain.Model.ValueObjects;

public readonly record struct BatteryThreshold
{
    public BatteryThreshold(decimal percentage)
    {
        if (percentage is < 0 or > 100)
            throw new DomainException("Battery threshold must be between 0 and 100.");

        Percentage = percentage;
    }

    public decimal Percentage { get; }
}
