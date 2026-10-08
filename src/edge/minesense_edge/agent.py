"""Edge agent loop: sense -> classify locally -> alarm -> buffer -> sync."""

from __future__ import annotations

import logging
import time
import uuid
from datetime import datetime, timezone
from typing import Callable

from .buffer import OfflineBuffer
from .classifier import RiskLevel, classify
from .signals import BiometricReading, SignalGenerator

logger = logging.getLogger(__name__)


def log_cabin_alarm(reading: BiometricReading, risk: RiskLevel) -> None:
    """Stand-in for the in-cabin buzzer/display; works without connectivity."""
    logger.warning(
        "IN-CABIN ALARM (%s): PERCLOS=%.2f blink=%.1f/min HRV=%.1f ms",
        risk.value,
        reading.perclos,
        reading.blink_rate_per_minute,
        reading.heart_rate_variability_ms,
    )


class EdgeAgent:
    def __init__(
        self,
        operator_id: str,
        monitoring_session_id: str,
        generator: SignalGenerator,
        buffer: OfflineBuffer,
        deliver: Callable[[dict], None],
        publisher=None,
        alarm: Callable[[BiometricReading, RiskLevel], None] = log_cabin_alarm,
    ) -> None:
        self._operator_id = operator_id
        self._session_id = monitoring_session_id
        self._generator = generator
        self._buffer = buffer
        self._deliver = deliver
        self._publisher = publisher
        self._alarm = alarm

    def build_event(self, reading: BiometricReading, risk: RiskLevel) -> dict:
        return {
            "id": str(uuid.uuid4()),
            "createdAt": datetime.now(timezone.utc).isoformat(),
            "localRiskLevel": risk.value,
            "assessment": {
                "operatorId": self._operator_id,
                "monitoringSessionId": self._session_id,
                "perclos": reading.perclos,
                "blinkRatePerMinute": reading.blink_rate_per_minute,
                "heartRateVariabilityMs": reading.heart_rate_variability_ms,
            },
        }

    def tick(self) -> RiskLevel:
        reading = self._generator.next_reading()
        risk = classify(reading)
        logger.info(
            "[%s] PERCLOS=%.2f blink=%.1f/min HRV=%.1f ms -> %s",
            self._generator.current_profile.name,
            reading.perclos,
            reading.blink_rate_per_minute,
            reading.heart_rate_variability_ms,
            risk.value,
        )

        if risk is RiskLevel.CRITICAL:
            self._alarm(reading, risk)

        if self._publisher is not None:
            self._publisher.publish({**reading.to_dict(), "riskLevel": risk.value,
                                     "operatorId": self._operator_id})

        self._buffer.append(self.build_event(reading, risk))
        self._buffer.flush(self._deliver)
        return risk

    def run(self, interval_seconds: float, max_iterations: int = 0,
            sleep: Callable[[float], None] = time.sleep) -> None:
        iteration = 0
        while max_iterations <= 0 or iteration < max_iterations:
            self.tick()
            iteration += 1
            if max_iterations <= 0 or iteration < max_iterations:
                sleep(interval_seconds)
