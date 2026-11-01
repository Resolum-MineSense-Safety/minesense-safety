"""Edge agent loop: sense -> classify locally -> warn -> store -> synchronize.

Detection and warnings run entirely in the cabin (US13): the network is only
used in :meth:`EdgeAgent.synchronize`, which never raises and is skipped while
the connectivity monitor is backing off.
"""

from __future__ import annotations

import logging
import threading
import time
from typing import Callable

from .buffer import OfflineBuffer
from .cabin_warnings import CabinWarning, WarningDispatcher
from .classifier import RiskLevel, classify
from .cloud_client import SyncClient
from .connectivity import CLOUD_DEPENDENT_FUNCTIONS, ConnectionState, ConnectivityMonitor
from .errors import CloudRejectedError, CloudUnavailableError
from .signals import BiometricReading, SignalGenerator

logger = logging.getLogger(__name__)

READING_EVENT = "FatigueReadingClassified"
WARNING_EVENT = "CabinWarningIssued"

WARNING_MESSAGES = {
    RiskLevel.WARNING: "Signos de fatiga detectados. Tome una pausa activa en cuanto sea seguro.",
    RiskLevel.CRITICAL: "Posible microsueño. Detenga la maquinaria en un lugar seguro.",
}
PRIORITY = {RiskLevel.NORMAL: 0, RiskLevel.WARNING: 1, RiskLevel.CRITICAL: 2}


class EdgeAgent:
    def __init__(
        self,
        operator_id: str,
        monitoring_session_id: str,
        generator: SignalGenerator,
        buffer: OfflineBuffer,
        client: SyncClient,
        dispatcher: WarningDispatcher,
        monitor: ConnectivityMonitor | None = None,
        publisher=None,
        batch_size: int = 100,
        max_batches_per_tick: int = 5,
    ) -> None:
        self._operator_id = operator_id
        self._session_id = monitoring_session_id
        self._generator = generator
        self._buffer = buffer
        self._client = client
        self._dispatcher = dispatcher
        self._monitor = monitor or ConnectivityMonitor()
        self._publisher = publisher
        self._batch_size = batch_size
        self._max_batches = max(1, max_batches_per_tick)

    @property
    def monitor(self) -> ConnectivityMonitor:
        return self._monitor

    # ---------- local protection (no network) ----------
    def process(self, reading: BiometricReading) -> RiskLevel:
        risk = classify(reading)
        offline = self._monitor.state is ConnectionState.OFFLINE
        logger.info("PERCLOS=%.2f blink=%.1f/min HRV=%.1f ms -> %s%s",
                    reading.perclos, reading.blink_rate_per_minute,
                    reading.heart_rate_variability_ms, risk.value, " (offline)" if offline else "")

        if risk is not RiskLevel.NORMAL:
            result = self._dispatcher.dispatch(CabinWarning(
                severity=risk.value, message=WARNING_MESSAGES[risk], perclos=reading.perclos,
                blink_rate_per_minute=reading.blink_rate_per_minute,
                heart_rate_variability_ms=reading.heart_rate_variability_ms))
            self._buffer.append(WARNING_EVENT, {
                "operatorId": self._operator_id,
                "monitoringSessionId": self._session_id,
                "severity": risk.value,
                "message": WARNING_MESSAGES[risk],
                "deliveredChannels": result.delivered,
                "unavailableChannels": result.unavailable,
                "issuedOffline": offline,
            }, occurred_at=reading.captured_at, priority=PRIORITY[risk])

        self._buffer.append(READING_EVENT, {
            "operatorId": self._operator_id,
            "monitoringSessionId": self._session_id,
            "perclos": reading.perclos,
            "blinkRatePerMinute": reading.blink_rate_per_minute,
            "heartRateVariabilityMs": reading.heart_rate_variability_ms,
            "riskLevel": risk.value,
            "classifiedOffline": offline,
        }, occurred_at=reading.captured_at, priority=PRIORITY[risk])

        if self._publisher is not None:
            self._publisher.publish({**reading.to_dict(), "riskLevel": risk.value,
                                     "operatorId": self._operator_id})
        return risk

    # ---------- synchronization (best effort) ----------
    def synchronize(self, force: bool = False) -> int:
        """Sends pending batches oldest-first; returns how many events were delivered."""
        if not force and not self._monitor.should_attempt():
            return 0
        delivered = 0
        for _ in range(self._max_batches):
            batch = self._buffer.next_batch(self._batch_size)
            if batch is None:
                break
            self._buffer.record_attempt(batch.batch_id)
            try:
                result = self._client.submit(batch)
            except CloudUnavailableError as error:
                if self._monitor.record_failure(error):
                    self._report_unavailable_functions()
                logger.info("%d event(s) kept in the offline buffer", len(self._buffer))
                break
            except CloudRejectedError as error:
                moved = self._buffer.mark_rejected(batch.batch_id, str(error))
                logger.error("Batch %s rejected, %d event(s) moved to dead letters: %s",
                             batch.batch_id, moved, error)
                self._on_success()
                continue
            delivered += self._buffer.mark_delivered(batch.batch_id)
            logger.info("Batch %s (sequence %d, %d events) %s", batch.batch_id, batch.sequence_number,
                        len(batch.events), "already stored" if result.duplicate else "accepted")
            self._on_success()
        return delivered

    def tick(self) -> RiskLevel:
        """One reading processed and synchronized in the caller's thread (tests, one-shot runs)."""
        risk = self.process(self._generator.next_reading())
        self.synchronize()
        return risk

    def run(self, interval_seconds: float, max_iterations: int = 0,
            sleep: Callable[[float], None] = time.sleep, background_sync: bool = True) -> None:
        """Detection loop. With ``background_sync`` the network never delays a reading:
        a worker thread synchronizes while the loop keeps sensing and warning."""
        worker = SyncWorker(self, max(interval_seconds, 0.1)) if background_sync else None
        if worker:
            worker.start()
        try:
            iteration = 0
            while max_iterations <= 0 or iteration < max_iterations:
                self.process(self._generator.next_reading())
                if worker is None:
                    self.synchronize()
                iteration += 1
                if max_iterations <= 0 or iteration < max_iterations:
                    sleep(interval_seconds)
        finally:
            if worker:
                worker.stop()

    def _on_success(self) -> None:
        if self._monitor.record_success():
            self._dispatcher.notify("Comunicación con el centro de control restablecida. "
                                    "Sincronizando eventos pendientes.")

    def _report_unavailable_functions(self) -> None:
        self._dispatcher.notify(
            "Sin comunicación con el centro de control. La detección de fatiga y las advertencias "
            "en cabina siguen activas. No disponible temporalmente: "
            + "; ".join(CLOUD_DEPENDENT_FUNCTIONS) + ".")


class SyncWorker:
    """Runs :meth:`EdgeAgent.synchronize` periodically in a daemon thread."""

    def __init__(self, agent: EdgeAgent, period_seconds: float) -> None:
        self._agent = agent
        self._period = period_seconds
        self._stop = threading.Event()
        self._thread = threading.Thread(target=self._loop, name="edge-sync", daemon=True)

    def start(self) -> None:
        self._thread.start()

    def stop(self, timeout: float = 10.0) -> None:
        self._stop.set()
        self._thread.join(timeout)

    def _loop(self) -> None:
        while not self._stop.is_set():
            try:
                self._agent.synchronize()
            except Exception:  # noqa: BLE001 - the worker must survive any bug in one attempt
                logger.exception("Unexpected error while synchronizing; retrying later")
            self._stop.wait(self._period)
