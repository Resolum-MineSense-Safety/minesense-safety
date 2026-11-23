"""Local fatigue risk classifier.

Mirrors src/backend/FatigueDetectionService/Domain/Services/FatigueRiskPolicy.cs
so the cabin alarm keeps working when the cloud is unreachable (EP06).
Keep both in sync when thresholds are recalibrated.
"""

from __future__ import annotations

from enum import Enum

from .signals import BiometricReading

CRITICAL_PERCLOS = 0.40
CRITICAL_COMBINED_PERCLOS = 0.25
CRITICAL_HEART_RATE_VARIABILITY_MS = 20
WARNING_PERCLOS = 0.15
WARNING_BLINK_RATE_PER_MINUTE = 25
WARNING_HEART_RATE_VARIABILITY_MS = 30


class RiskLevel(str, Enum):
    NORMAL = "Normal"
    WARNING = "Warning"
    CRITICAL = "Critical"


def classify(reading: BiometricReading) -> RiskLevel:
    if reading.perclos >= CRITICAL_PERCLOS or (
        reading.perclos >= CRITICAL_COMBINED_PERCLOS
        and reading.heart_rate_variability_ms < CRITICAL_HEART_RATE_VARIABILITY_MS
    ):
        return RiskLevel.CRITICAL

    if (
        reading.perclos >= WARNING_PERCLOS
        or reading.blink_rate_per_minute >= WARNING_BLINK_RATE_PER_MINUTE
        or reading.heart_rate_variability_ms < WARNING_HEART_RATE_VARIABILITY_MS
    ):
        return RiskLevel.WARNING

    return RiskLevel.NORMAL
