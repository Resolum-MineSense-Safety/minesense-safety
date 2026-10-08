"""Agent configuration read from environment variables."""

from __future__ import annotations

import os
import uuid
from dataclasses import dataclass
from typing import Mapping


@dataclass(frozen=True)
class EdgeConfig:
    fatigue_service_url: str
    alert_service_url: str
    operator_id: str
    monitoring_session_id: str
    profile: str
    cycle_length: int
    interval_seconds: float
    max_iterations: int
    buffer_path: str
    http_timeout_seconds: float
    mqtt_host: str | None
    mqtt_port: int
    mqtt_topic: str
    random_seed: int | None

    @classmethod
    def from_env(cls, env: Mapping[str, str] | None = None) -> "EdgeConfig":
        env = os.environ if env is None else env
        operator_id = env.get("EDGE_OPERATOR_ID") or str(uuid.uuid4())
        seed = env.get("EDGE_RANDOM_SEED")
        return cls(
            fatigue_service_url=env.get("FATIGUE_SERVICE_URL", "http://localhost:5158"),
            alert_service_url=env.get("ALERT_SERVICE_URL", "http://localhost:5233"),
            operator_id=operator_id,
            monitoring_session_id=env.get("EDGE_MONITORING_SESSION_ID") or str(uuid.uuid4()),
            profile=env.get("EDGE_PROFILE", "cycle"),
            cycle_length=int(env.get("EDGE_CYCLE_LENGTH", "10")),
            interval_seconds=float(env.get("EDGE_INTERVAL_SECONDS", "2")),
            max_iterations=int(env.get("EDGE_MAX_ITERATIONS", "0")),
            buffer_path=env.get("EDGE_BUFFER_PATH", "data/offline-buffer.json"),
            http_timeout_seconds=float(env.get("EDGE_HTTP_TIMEOUT_SECONDS", "3")),
            mqtt_host=env.get("MQTT_HOST") or None,
            mqtt_port=int(env.get("MQTT_PORT", "1883")),
            mqtt_topic=env.get("MQTT_TOPIC", f"minesense/cabins/{operator_id}/signals"),
            random_seed=int(seed) if seed else None,
        )
