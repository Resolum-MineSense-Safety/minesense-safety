"""Agent configuration read from environment variables (no secrets in code)."""

from __future__ import annotations

import os
from dataclasses import dataclass
from typing import Mapping

MAX_BATCH_SIZE = 500  # SynchronizationBatch accepts between 1 and 500 events


def _split(value: str | None) -> tuple[str, ...]:
    return tuple(item.strip().lower() for item in (value or "").split(",") if item.strip())


@dataclass(frozen=True)
class EdgeConfig:
    sync_service_url: str
    identity_url: str | None
    access_token: str | None
    username: str | None
    password: str | None
    edge_node_id: str | None
    device_id: str | None
    operator_id: str | None
    monitoring_session_id: str | None
    profile: str
    cycle_length: int
    interval_seconds: float
    max_iterations: int
    buffer_path: str
    max_buffered_events: int
    batch_size: int
    max_batches_per_tick: int
    retry_initial_seconds: float
    retry_max_seconds: float
    http_timeout_seconds: float
    unavailable_warning_channels: tuple[str, ...]
    mqtt_host: str | None
    mqtt_port: int
    mqtt_topic: str | None
    random_seed: int | None

    @classmethod
    def from_env(cls, env: Mapping[str, str] | None = None) -> "EdgeConfig":
        env = os.environ if env is None else env
        seed = env.get("EDGE_RANDOM_SEED")
        batch_size = int(env.get("EDGE_BATCH_SIZE", "100"))
        if not 1 <= batch_size <= MAX_BATCH_SIZE:
            raise ValueError(f"EDGE_BATCH_SIZE must be between 1 and {MAX_BATCH_SIZE}")
        return cls(
            sync_service_url=env.get("EDGE_SYNC_SERVICE_URL", "http://localhost:5280"),
            identity_url=env.get("IDENTITY_URL") or None,
            access_token=env.get("EDGE_ACCESS_TOKEN") or None,
            username=env.get("EDGE_USERNAME") or None,
            password=env.get("EDGE_PASSWORD") or None,
            edge_node_id=env.get("EDGE_NODE_ID") or None,
            device_id=env.get("EDGE_DEVICE_ID") or None,
            operator_id=env.get("EDGE_OPERATOR_ID") or None,
            monitoring_session_id=env.get("EDGE_MONITORING_SESSION_ID") or None,
            profile=env.get("EDGE_PROFILE", "cycle"),
            cycle_length=int(env.get("EDGE_CYCLE_LENGTH", "10")),
            interval_seconds=float(env.get("EDGE_INTERVAL_SECONDS", "2")),
            max_iterations=int(env.get("EDGE_MAX_ITERATIONS", "0")),
            buffer_path=env.get("EDGE_BUFFER_PATH", "data/edge-buffer.db"),
            max_buffered_events=int(env.get("EDGE_MAX_BUFFERED_EVENTS", "100000")),
            batch_size=batch_size,
            max_batches_per_tick=int(env.get("EDGE_MAX_BATCHES_PER_TICK", "5")),
            retry_initial_seconds=float(env.get("EDGE_RETRY_INITIAL_SECONDS", "2")),
            retry_max_seconds=float(env.get("EDGE_RETRY_MAX_SECONDS", "60")),
            http_timeout_seconds=float(env.get("EDGE_HTTP_TIMEOUT_SECONDS", "3")),
            unavailable_warning_channels=_split(env.get("EDGE_UNAVAILABLE_WARNING_CHANNELS")),
            mqtt_host=env.get("MQTT_HOST") or None,
            mqtt_port=int(env.get("MQTT_PORT", "1883")),
            mqtt_topic=env.get("MQTT_TOPIC") or None,
            random_seed=int(seed) if seed else None,
        )
