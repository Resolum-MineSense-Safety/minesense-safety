import pytest

from minesense_edge.classifier import RiskLevel, classify
from minesense_edge.signals import BiometricReading


def reading(perclos, blink_rate, hrv):
    return BiometricReading(perclos, blink_rate, hrv)


# Same cases as FatigueRiskPolicyTests.cs in the FatigueDetectionService tests
def test_rested_signals_return_normal():
    assert classify(reading(0.10, 15, 50)) is RiskLevel.NORMAL


@pytest.mark.parametrize(
    "perclos, blink_rate, hrv",
    [(0.15, 15, 50), (0.10, 25, 50), (0.10, 15, 29)],
)
def test_signals_at_warning_threshold_return_warning(perclos, blink_rate, hrv):
    assert classify(reading(perclos, blink_rate, hrv)) is RiskLevel.WARNING


def test_perclos_at_critical_threshold_returns_critical():
    assert classify(reading(0.40, 15, 50)) is RiskLevel.CRITICAL


def test_moderate_perclos_with_low_hrv_returns_critical():
    assert classify(reading(0.25, 15, 19)) is RiskLevel.CRITICAL


@pytest.mark.parametrize(
    "perclos, blink_rate, hrv, expected",
    [
        (0.149, 24.9, 30, RiskLevel.NORMAL),   # just below every warning threshold
        (0.399, 15, 50, RiskLevel.WARNING),    # just below critical PERCLOS
        (0.25, 15, 20, RiskLevel.WARNING),     # HRV at 20 is not < 20
        (0.249, 15, 10, RiskLevel.WARNING),    # PERCLOS below combined threshold
    ],
)
def test_boundaries(perclos, blink_rate, hrv, expected):
    assert classify(reading(perclos, blink_rate, hrv)) is expected


def test_risk_level_values_match_backend_enum_names():
    assert [level.value for level in RiskLevel] == ["Normal", "Warning", "Critical"]
