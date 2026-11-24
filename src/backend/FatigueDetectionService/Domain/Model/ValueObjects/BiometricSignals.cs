using MineSenseSafety.Shared.Domain.Model;

namespace FatigueDetectionService.Domain.Model.ValueObjects;

/// <summary>
/// Biometric readings captured by the in-cabin sensors for a single assessment.
/// </summary>
public record BiometricSignals
{
    /// <summary>Percentage of eyelid closure over time, from 0 to 1.</summary>
    public double Perclos { get; }
    public double BlinkRatePerMinute { get; }
    public double HeartRateVariabilityMs { get; }

    public BiometricSignals(double perclos, double blinkRatePerMinute, double heartRateVariabilityMs)
    {
        if (perclos is < 0 or > 1)
            throw new DomainException("PERCLOS must be between 0 and 1.");
        if (blinkRatePerMinute < 0)
            throw new DomainException("Blink rate cannot be negative.");
        if (heartRateVariabilityMs <= 0)
            throw new DomainException("Heart rate variability must be greater than zero.");

        Perclos = perclos;
        BlinkRatePerMinute = blinkRatePerMinute;
        HeartRateVariabilityMs = heartRateVariabilityMs;
    }
}
