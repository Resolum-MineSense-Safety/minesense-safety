using FatigueDetectionService.Domain.Model.ValueObjects;

namespace FatigueDetectionService.Domain.Services;

/// <summary>
/// Classifies biometric signals into a fatigue risk level (EP02).
/// Thresholds are reference values to be calibrated with the Edge inference model.
/// </summary>
public static class FatigueRiskPolicy
{
    private const double CriticalPerclos = 0.40;
    private const double CriticalCombinedPerclos = 0.25;
    private const double CriticalHeartRateVariabilityMs = 20;
    private const double WarningPerclos = 0.15;
    private const double WarningBlinkRatePerMinute = 25;
    private const double WarningHeartRateVariabilityMs = 30;

    public static RiskLevel Classify(BiometricSignals signals)
    {
        if (signals.Perclos >= CriticalPerclos ||
            (signals.Perclos >= CriticalCombinedPerclos &&
             signals.HeartRateVariabilityMs < CriticalHeartRateVariabilityMs))
            return RiskLevel.Critical;

        if (signals.Perclos >= WarningPerclos ||
            signals.BlinkRatePerMinute >= WarningBlinkRatePerMinute ||
            signals.HeartRateVariabilityMs < WarningHeartRateVariabilityMs)
            return RiskLevel.Warning;

        return RiskLevel.Normal;
    }
}
