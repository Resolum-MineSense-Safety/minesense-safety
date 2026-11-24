"""In-cabin warning channels (US05 in the cabin, US13 offline).

Warnings are produced and delivered locally, so they never depend on the
control center. The dispatcher tries every configured channel; when one is
unavailable (e.g. the buzzer is disconnected) the operator is still warned by
the others and is told which channel failed. If every channel fails the
warning is written to the agent log at CRITICAL level as a last resort, so the
event is never lost silently.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass, field
from typing import Iterable, Protocol

logger = logging.getLogger(__name__)
cabin_logger = logging.getLogger("minesense_edge.cabin")


class WarningChannelUnavailable(Exception):
    """The physical or simulated warning device cannot be used right now."""


@dataclass(frozen=True)
class CabinWarning:
    severity: str          # Warning | Critical
    message: str
    perclos: float
    blink_rate_per_minute: float
    heart_rate_variability_ms: float


class WarningChannel(Protocol):
    name: str

    def emit(self, warning: CabinWarning) -> None: ...

    def notify(self, message: str) -> None: ...


class CabinDisplayChannel:
    """Stand-in for the in-cabin screen."""

    name = "display"

    def __init__(self, available: bool = True) -> None:
        self.available = available

    def emit(self, warning: CabinWarning) -> None:
        self._check()
        cabin_logger.warning("[PANTALLA] %s: %s (PERCLOS=%.2f parpadeo=%.1f/min HRV=%.1f ms)",
                             warning.severity.upper(), warning.message, warning.perclos,
                             warning.blink_rate_per_minute, warning.heart_rate_variability_ms)

    def notify(self, message: str) -> None:
        self._check()
        cabin_logger.warning("[PANTALLA] %s", message)

    def _check(self) -> None:
        if not self.available:
            raise WarningChannelUnavailable("cabin display not responding")


class BuzzerChannel:
    """Stand-in for the in-cabin buzzer; it only sounds for critical warnings."""

    name = "buzzer"

    def __init__(self, available: bool = True) -> None:
        self.available = available

    def emit(self, warning: CabinWarning) -> None:
        if not self.available:
            raise WarningChannelUnavailable("buzzer not responding")
        if warning.severity == "Critical":
            cabin_logger.warning("[ALARMA SONORA] microsueño probable, detenga la maquinaria")

    def notify(self, message: str) -> None:
        return None  # informative notices are not sounded


@dataclass
class DispatchResult:
    delivered: list[str] = field(default_factory=list)
    unavailable: list[str] = field(default_factory=list)
    fallback_used: bool = False


class WarningDispatcher:
    def __init__(self, channels: Iterable[WarningChannel]) -> None:
        self._channels = list(channels)
        self._known_unavailable: set[str] = set()

    @property
    def unavailable_channels(self) -> list[str]:
        return sorted(self._known_unavailable)

    def dispatch(self, warning: CabinWarning) -> DispatchResult:
        result = DispatchResult()
        for channel in self._channels:
            try:
                channel.emit(warning)
                result.delivered.append(channel.name)
                self._mark_available(channel.name)
            except Exception as error:  # noqa: BLE001 - one broken device must not stop the others
                result.unavailable.append(channel.name)
                self._mark_unavailable(channel.name, error)
        if not result.delivered:
            result.fallback_used = True
            logger.critical("NO WARNING CHANNEL AVAILABLE - %s warning for the operator: %s",
                            warning.severity, warning.message)
        return result

    def notify(self, message: str) -> list[str]:
        """Shows an informative notice (e.g. functions unavailable) on every working channel."""
        delivered = []
        for channel in self._channels:
            try:
                channel.notify(message)
                delivered.append(channel.name)
            except Exception:  # noqa: BLE001
                continue
        if not delivered:
            logger.error("Notice not shown in the cabin (no channel): %s", message)
        return delivered

    def _mark_unavailable(self, name: str, error: Exception) -> None:
        if name in self._known_unavailable:
            return
        self._known_unavailable.add(name)
        logger.error("Warning channel %s unavailable: %s", name, error)
        self.notify(f"Medio de advertencia no disponible: {name}. "
                    "Las advertencias continúan por los medios restantes.")

    def _mark_available(self, name: str) -> None:
        if name in self._known_unavailable:
            self._known_unavailable.discard(name)
            logger.info("Warning channel %s available again", name)


def create_channels(unavailable: Iterable[str] = ()) -> list[WarningChannel]:
    """Default cabin channels; ``unavailable`` simulates disconnected devices."""
    disabled = {name.lower() for name in unavailable}
    return [CabinDisplayChannel(available="display" not in disabled),
            BuzzerChannel(available="buzzer" not in disabled)]
