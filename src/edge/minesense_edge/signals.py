"""Synthetic biometric signal generator with scenario profiles."""

from __future__ import annotations

import random
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone


@dataclass(frozen=True)
class BiometricReading:
    """One sample of the in-cabin sensors (same fields as AssessFatigueResource)."""

    perclos: float
    blink_rate_per_minute: float
    heart_rate_variability_ms: float
    captured_at: str = field(
        default_factory=lambda: datetime.now(timezone.utc).isoformat()
    )

    def to_dict(self) -> dict:
        return asdict(self)


@dataclass(frozen=True)
class ScenarioProfile:
    """Value ranges (inclusive) used to draw synthetic readings."""

    name: str
    perclos: tuple[float, float]
    blink_rate_per_minute: tuple[float, float]
    heart_rate_variability_ms: tuple[float, float]


# Ranges are chosen so every reading of a profile falls in a single risk level
# of FatigueRiskPolicy: normal -> Normal, drowsy -> Warning, microsleep -> Critical.
PROFILES: dict[str, ScenarioProfile] = {
    "normal": ScenarioProfile("normal", (0.02, 0.12), (8.0, 20.0), (40.0, 80.0)),
    "drowsy": ScenarioProfile("drowsy", (0.15, 0.35), (18.0, 30.0), (22.0, 35.0)),
    "microsleep": ScenarioProfile("microsleep", (0.40, 0.75), (5.0, 15.0), (10.0, 25.0)),
}

CYCLE_ORDER = ("normal", "drowsy", "microsleep")


class SignalGenerator:
    """Draws readings from a profile, or rotates profiles when profile == "cycle"."""

    def __init__(
        self,
        profile: str = "normal",
        rng: random.Random | None = None,
        cycle_length: int = 10,
    ) -> None:
        if profile != "cycle" and profile not in PROFILES:
            raise ValueError(
                f"Unknown profile '{profile}'. Use one of: "
                f"{', '.join([*PROFILES, 'cycle'])}"
            )
        if cycle_length < 1:
            raise ValueError("cycle_length must be >= 1")
        self._profile = profile
        self._rng = rng or random.Random()
        self._cycle_length = cycle_length
        self._count = 0

    @property
    def current_profile(self) -> ScenarioProfile:
        if self._profile != "cycle":
            return PROFILES[self._profile]
        index = (self._count // self._cycle_length) % len(CYCLE_ORDER)
        return PROFILES[CYCLE_ORDER[index]]

    def next_reading(self) -> BiometricReading:
        profile = self.current_profile
        self._count += 1
        return BiometricReading(
            perclos=round(self._rng.uniform(*profile.perclos), 3),
            blink_rate_per_minute=round(self._rng.uniform(*profile.blink_rate_per_minute), 1),
            heart_rate_variability_ms=round(
                self._rng.uniform(*profile.heart_rate_variability_ms), 1
            ),
        )
