"""Connectivity state with the control center (US13 T01, US14 T04).

The agent never blocks on the network: it asks the monitor whether a sync
attempt is due, and reports each success or failure. Failures back off
exponentially so a cabin without coverage does not waste CPU or battery.
"""

from __future__ import annotations

import logging
import time
from enum import Enum
from typing import Callable

logger = logging.getLogger(__name__)

# Functions of the solution that depend on the control center (US13, scenario 3).
CLOUD_DEPENDENT_FUNCTIONS = (
    "notificación al supervisor en el centro de control",
    "estado del operador en el panel de flota",
    "historial central de eventos (se completará al reconectar)",
)


class ConnectionState(str, Enum):
    UNKNOWN = "unknown"
    ONLINE = "online"
    OFFLINE = "offline"


class ConnectivityMonitor:
    def __init__(self, initial_backoff_seconds: float = 2.0, max_backoff_seconds: float = 60.0,
                 clock: Callable[[], float] = time.monotonic) -> None:
        self._initial = max(initial_backoff_seconds, 0.0)
        self._max = max(max_backoff_seconds, self._initial)
        self._clock = clock
        self._state = ConnectionState.UNKNOWN
        self._backoff = self._initial
        self._next_attempt = 0.0
        self._offline_since: float | None = None
        self._last_error: str | None = None

    @property
    def state(self) -> ConnectionState:
        return self._state

    @property
    def last_error(self) -> str | None:
        return self._last_error

    def should_attempt(self) -> bool:
        return self._state is not ConnectionState.OFFLINE or self._clock() >= self._next_attempt

    def record_success(self) -> bool:
        """Returns True when this success restores a lost connection."""
        restored = self._state is ConnectionState.OFFLINE
        if restored:
            logger.warning("Communication with the control center restored after %.0f s",
                           self._clock() - (self._offline_since or self._clock()))
        self._state = ConnectionState.ONLINE
        self._backoff = self._initial
        self._offline_since = None
        self._last_error = None
        return restored

    def record_failure(self, error: Exception | str) -> bool:
        """Returns True when this failure means the connection has just been lost."""
        lost = self._state is not ConnectionState.OFFLINE
        self._last_error = str(error)
        now = self._clock()
        if lost:
            self._state = ConnectionState.OFFLINE
            self._offline_since = now
            self._backoff = self._initial
            logger.warning("Communication with the control center lost: %s", error)
        else:
            self._backoff = min(self._backoff * 2, self._max) if self._backoff else self._max
        self._next_attempt = now + self._backoff
        return lost
