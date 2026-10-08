"""Offline-first event buffer persisted to a JSON file (EP06).

Events are kept in FIFO order while the cloud is unreachable and flushed in
the same order when connectivity returns. The file is rewritten atomically so
a power loss in the cabin never leaves a half-written buffer.
"""

from __future__ import annotations

import json
import logging
import os
import tempfile
from collections import deque
from pathlib import Path
from typing import Callable

from .errors import CloudRejectedError, CloudUnavailableError

logger = logging.getLogger(__name__)

Sender = Callable[[dict], None]


class OfflineBuffer:
    def __init__(self, path: str | os.PathLike, max_events: int = 10_000) -> None:
        if max_events < 1:
            raise ValueError("max_events must be >= 1")
        self._path = Path(path)
        self._max_events = max_events
        self._events: deque[dict] = deque(self._load())

    def __len__(self) -> int:
        return len(self._events)

    @property
    def path(self) -> Path:
        return self._path

    def snapshot(self) -> list[dict]:
        return [dict(event) for event in self._events]

    def append(self, event: dict) -> None:
        if len(self._events) >= self._max_events:
            dropped = self._events.popleft()
            logger.warning("Offline buffer full, dropping oldest event %s", dropped.get("id"))
        self._events.append(event)
        self._persist()

    def flush(self, send: Sender) -> int:
        """Send events oldest-first; stop at the first unavailable error.

        ``send`` may mutate the event (e.g. remember a partial delivery); the
        mutation is persisted if delivery fails so it is not repeated.
        Returns the number of events removed from the buffer.
        """
        removed = 0
        while self._events:
            event = self._events[0]
            try:
                send(event)
            except CloudUnavailableError as error:
                self._persist()
                logger.info("Cloud unreachable, %d event(s) kept offline: %s", len(self._events), error)
                break
            except CloudRejectedError as error:
                logger.error("Cloud rejected event %s, discarding it: %s", event.get("id"), error)
            self._events.popleft()
            removed += 1
            self._persist()
        return removed

    def _load(self) -> list[dict]:
        if not self._path.exists():
            return []
        try:
            data = json.loads(self._path.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as error:
            logger.error("Could not read offline buffer %s, starting empty: %s", self._path, error)
            return []
        if not isinstance(data, list):
            logger.error("Offline buffer %s is not a list, starting empty", self._path)
            return []
        return data[-self._max_events:]

    def _persist(self) -> None:
        self._path.parent.mkdir(parents=True, exist_ok=True)
        fd, tmp_name = tempfile.mkstemp(dir=self._path.parent, prefix=".buffer-", suffix=".json")
        try:
            with os.fdopen(fd, "w", encoding="utf-8") as tmp:
                json.dump(list(self._events), tmp)
            os.replace(tmp_name, self._path)
        except BaseException:
            if os.path.exists(tmp_name):
                os.remove(tmp_name)
            raise
