import random

import pytest

from minesense_edge.classifier import RiskLevel, classify
from minesense_edge.signals import PROFILES, SignalGenerator

EXPECTED_RISK = {
    "normal": RiskLevel.NORMAL,
    "drowsy": RiskLevel.WARNING,
    "microsleep": RiskLevel.CRITICAL,
}


@pytest.mark.parametrize("name", list(PROFILES))
def test_profile_readings_stay_within_ranges(name):
    profile = PROFILES[name]
    generator = SignalGenerator(name, random.Random(42))
    for _ in range(500):
        r = generator.next_reading()
        assert profile.perclos[0] <= r.perclos <= profile.perclos[1]
        assert profile.blink_rate_per_minute[0] <= r.blink_rate_per_minute <= profile.blink_rate_per_minute[1]
        assert profile.heart_rate_variability_ms[0] <= r.heart_rate_variability_ms <= profile.heart_rate_variability_ms[1]
        # Always valid for the backend BiometricSignals value object
        assert 0 <= r.perclos <= 1 and r.blink_rate_per_minute >= 0 and r.heart_rate_variability_ms > 0


@pytest.mark.parametrize("name, expected", EXPECTED_RISK.items())
def test_profile_maps_to_a_single_risk_level(name, expected):
    generator = SignalGenerator(name, random.Random(7))
    assert {classify(generator.next_reading()) for _ in range(500)} == {expected}


def test_cycle_rotates_profiles():
    generator = SignalGenerator("cycle", random.Random(1), cycle_length=2)
    names = []
    for _ in range(7):
        names.append(generator.current_profile.name)
        generator.next_reading()
    assert names == ["normal", "normal", "drowsy", "drowsy", "microsleep", "microsleep", "normal"]


def test_same_seed_is_reproducible():
    a = SignalGenerator("drowsy", random.Random(3))
    b = SignalGenerator("drowsy", random.Random(3))
    assert [a.next_reading().perclos for _ in range(5)] == [b.next_reading().perclos for _ in range(5)]


def test_unknown_profile_is_rejected():
    with pytest.raises(ValueError):
        SignalGenerator("sleepy")
