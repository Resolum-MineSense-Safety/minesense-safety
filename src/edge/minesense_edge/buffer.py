"""Durable offline buffer backed by SQLite (US13, US14).

Every event detected in the cabin is written here first, so nothing is lost if
the cloud is unreachable or the gateway reboots. Events leave the buffer only
inside synchronization batches:

* A batch is *sealed* (BatchId, SequenceNumber and its events fixed) and stored
  before it is sent. If the upload fails, or the link drops before the answer
  arrives, the next attempt resends exactly the same batch with the same
  BatchId, which the Edge Synchronization Service treats as a duplicate.
* Batches are sent in SequenceNumber order and events keep their capture order,
  so the central history is rebuilt in the order the cabin produced it.
* A batch the service rejects as invalid (400/422) is moved to a dead-letter
  table for inspection instead of being silently discarded.
"""

from __future__ import annotations

import json
import logging
import os
import sqlite3
import threading
import uuid
from dataclasses import dataclass
from datetime import datetime, timezone
from functools import wraps
from pathlib import Path

logger = logging.getLogger(__name__)

SCHEMA = """
CREATE TABLE IF NOT EXISTS meta (
    key   TEXT PRIMARY KEY,
    value TEXT NOT NULL
);
CREATE TABLE IF NOT EXISTS batches (
    batch_id        TEXT PRIMARY KEY,
    sequence_number INTEGER NOT NULL UNIQUE,
    captured_at     TEXT NOT NULL,
    attempts        INTEGER NOT NULL DEFAULT 0
);
CREATE TABLE IF NOT EXISTS events (
    position    INTEGER PRIMARY KEY AUTOINCREMENT,
    event_id    TEXT NOT NULL UNIQUE,
    occurred_at TEXT NOT NULL,
    event_type  TEXT NOT NULL,
    payload     TEXT NOT NULL,
    priority    INTEGER NOT NULL DEFAULT 0,
    batch_id    TEXT REFERENCES batches(batch_id)
);
CREATE INDEX IF NOT EXISTS ix_events_batch ON events(batch_id, position);
CREATE TABLE IF NOT EXISTS dead_letters (
    event_id    TEXT PRIMARY KEY,
    batch_id    TEXT NOT NULL,
    occurred_at TEXT NOT NULL,
    event_type  TEXT NOT NULL,
    payload     TEXT NOT NULL,
    reason      TEXT NOT NULL,
    failed_at   TEXT NOT NULL
);
"""


@dataclass(frozen=True)
class BufferedEvent:
    event_id: str
    occurred_at: str
    event_type: str
    payload: dict

    def to_resource(self) -> dict:
        """SynchronizationEventResource of the Edge Synchronization Service."""
        return {
            "eventId": self.event_id,
            "occurredAt": self.occurred_at,
            "eventType": self.event_type,
            "payload": self.payload,
        }


@dataclass(frozen=True)
class SealedBatch:
    batch_id: str
    sequence_number: int
    captured_at: str
    attempts: int
    events: tuple[BufferedEvent, ...]


def _locked(method):
    """The detection loop writes while the sync worker reads: one connection, one lock."""
    @wraps(method)
    def wrapper(self, *args, **kwargs):
        with self._lock:
            return method(self, *args, **kwargs)
    return wrapper


def _now() -> str:
    return datetime.now(timezone.utc).isoformat()


class OfflineBuffer:
    def __init__(self, path: str | os.PathLike, max_events: int = 100_000) -> None:
        if max_events < 1:
            raise ValueError("max_events must be >= 1")
        self._path = Path(path)
        self._max_events = max_events
        self._path.parent.mkdir(parents=True, exist_ok=True)
        # isolation_level=None: explicit BEGIN IMMEDIATE ... COMMIT around each change.
        self._lock = threading.RLock()
        self._db = sqlite3.connect(str(self._path), isolation_level=None, check_same_thread=False)
        self._db.execute("PRAGMA journal_mode=WAL")
        self._db.execute("PRAGMA synchronous=FULL")  # survive a power cut in the cabin
        self._db.execute("PRAGMA foreign_keys=ON")
        self._db.executescript(SCHEMA)

    @property
    def path(self) -> Path:
        return self._path

    @_locked
    def close(self) -> None:
        self._db.close()

    @_locked
    def __len__(self) -> int:
        return self._db.execute("SELECT COUNT(*) FROM events").fetchone()[0]

    @_locked
    def dead_letter_count(self) -> int:
        return self._db.execute("SELECT COUNT(*) FROM dead_letters").fetchone()[0]

    # ---------- stable identity of the gateway ----------
    @_locked
    def get_or_create_id(self, key: str, preferred: str | None = None) -> str:
        """Returns the persisted id for ``key`` (edge node, device...), creating it once."""
        row = self._db.execute("SELECT value FROM meta WHERE key = ?", (key,)).fetchone()
        if preferred:
            if row is None or row[0] != preferred:
                self._db.execute("INSERT OR REPLACE INTO meta(key, value) VALUES (?, ?)", (key, preferred))
            return preferred
        if row is not None:
            return row[0]
        value = str(uuid.uuid4())
        self._db.execute("INSERT INTO meta(key, value) VALUES (?, ?)", (key, value))
        return value

    # ---------- writing ----------
    @_locked
    def append(self, event_type: str, payload: dict, occurred_at: str | None = None,
               priority: int = 0, event_id: str | None = None) -> str:
        """Stores one event durably and returns its EventId."""
        event_id = event_id or str(uuid.uuid4())
        with self._transaction():
            self._make_room()
            self._db.execute(
                "INSERT INTO events(event_id, occurred_at, event_type, payload, priority) "
                "VALUES (?, ?, ?, ?, ?)",
                (event_id, occurred_at or _now(), event_type, json.dumps(payload), priority),
            )
        return event_id

    def _make_room(self) -> None:
        count = self._db.execute("SELECT COUNT(*) FROM events").fetchone()[0]
        if count < self._max_events:
            return
        # Drop the oldest low-priority event that is not sealed in a batch yet;
        # warnings (priority > 0) are only dropped when nothing else is left.
        row = self._db.execute(
            "SELECT position, event_id FROM events WHERE batch_id IS NULL "
            "ORDER BY priority, position LIMIT 1").fetchone()
        if row is None:
            raise RuntimeError("Offline buffer is full of sealed batches; cannot store more events")
        self._db.execute("DELETE FROM events WHERE position = ?", (row[0],))
        logger.warning("Offline buffer full (%d events), dropped oldest low-priority event %s",
                       self._max_events, row[1])

    @_locked
    def pending_events(self, limit: int = 1000) -> list[BufferedEvent]:
        """Events not delivered yet, in the order they will be sent (does not seal batches)."""
        return [
            BufferedEvent(event_id, occurred_at, event_type, json.loads(payload))
            for event_id, occurred_at, event_type, payload in self._db.execute(
                "SELECT e.event_id, e.occurred_at, e.event_type, e.payload FROM events e "
                "LEFT JOIN batches b ON b.batch_id = e.batch_id "
                "ORDER BY b.sequence_number IS NULL, b.sequence_number, e.position LIMIT ?", (limit,))]

    # ---------- synchronization ----------
    @_locked
    def next_batch(self, batch_size: int) -> SealedBatch | None:
        """Returns the oldest sealed batch, sealing a new one from pending events if needed."""
        batch = self._oldest_sealed_batch()
        if batch is not None:
            return batch
        with self._transaction():
            positions = [row[0] for row in self._db.execute(
                "SELECT position FROM events WHERE batch_id IS NULL ORDER BY position LIMIT ?",
                (batch_size,))]
            if not positions:
                return None
            sequence = int(self._meta("next_sequence_number", "0"))
            batch_id = str(uuid.uuid4())
            self._db.execute(
                "INSERT INTO batches(batch_id, sequence_number, captured_at) VALUES (?, ?, ?)",
                (batch_id, sequence, _now()))
            self._db.executemany("UPDATE events SET batch_id = ? WHERE position = ?",
                                 [(batch_id, position) for position in positions])
            self._db.execute("INSERT OR REPLACE INTO meta(key, value) VALUES ('next_sequence_number', ?)",
                             (str(sequence + 1),))
        return self._oldest_sealed_batch()

    @_locked
    def record_attempt(self, batch_id: str) -> None:
        self._db.execute("UPDATE batches SET attempts = attempts + 1 WHERE batch_id = ?", (batch_id,))

    @_locked
    def mark_delivered(self, batch_id: str) -> int:
        with self._transaction():
            removed = self._db.execute("DELETE FROM events WHERE batch_id = ?", (batch_id,)).rowcount
            self._db.execute("DELETE FROM batches WHERE batch_id = ?", (batch_id,))
        return removed

    @_locked
    def mark_rejected(self, batch_id: str, reason: str) -> int:
        with self._transaction():
            self._db.execute(
                "INSERT OR REPLACE INTO dead_letters(event_id, batch_id, occurred_at, event_type, payload, "
                "reason, failed_at) SELECT event_id, batch_id, occurred_at, event_type, payload, ?, ? "
                "FROM events WHERE batch_id = ?", (reason[:500], _now(), batch_id))
            moved = self._db.execute("DELETE FROM events WHERE batch_id = ?", (batch_id,)).rowcount
            self._db.execute("DELETE FROM batches WHERE batch_id = ?", (batch_id,))
        return moved

    # ---------- helpers ----------
    def _oldest_sealed_batch(self) -> SealedBatch | None:
        row = self._db.execute(
            "SELECT batch_id, sequence_number, captured_at, attempts FROM batches "
            "ORDER BY sequence_number LIMIT 1").fetchone()
        if row is None:
            return None
        events = tuple(
            BufferedEvent(event_id, occurred_at, event_type, json.loads(payload))
            for event_id, occurred_at, event_type, payload in self._db.execute(
                "SELECT event_id, occurred_at, event_type, payload FROM events "
                "WHERE batch_id = ? ORDER BY position", (row[0],)))
        return SealedBatch(row[0], row[1], row[2], row[3], events)

    def _meta(self, key: str, default: str) -> str:
        row = self._db.execute("SELECT value FROM meta WHERE key = ?", (key,)).fetchone()
        return row[0] if row else default

    def _transaction(self) -> "_Transaction":
        return _Transaction(self._db)


class _Transaction:
    def __init__(self, db: sqlite3.Connection) -> None:
        self._db = db

    def __enter__(self) -> None:
        self._db.execute("BEGIN IMMEDIATE")

    def __exit__(self, exc_type, exc, tb) -> None:
        self._db.execute("COMMIT" if exc_type is None else "ROLLBACK")
