"""Optional MQTT publisher for raw readings (best effort, never blocks the loop)."""

from __future__ import annotations

import json
import logging

logger = logging.getLogger(__name__)

try:  # paho-mqtt is optional: HTTP is the required synchronization path
    import paho.mqtt.client as mqtt
except ImportError:  # pragma: no cover - depends on the environment
    mqtt = None


class NullPublisher:
    def publish(self, payload: dict) -> None:
        return None

    def close(self) -> None:
        return None


class MqttPublisher:
    def __init__(self, host: str, port: int, topic: str, client_id: str) -> None:
        if mqtt is None:
            raise RuntimeError("paho-mqtt is not installed")
        self._topic = topic
        try:
            self._client = mqtt.Client(mqtt.CallbackAPIVersion.VERSION2, client_id=client_id)
        except AttributeError:  # paho-mqtt < 2.0
            self._client = mqtt.Client(client_id=client_id)
        self._client.connect_async(host, port)
        self._client.loop_start()

    def publish(self, payload: dict) -> None:
        try:
            self._client.publish(self._topic, json.dumps(payload), qos=0)
        except Exception as error:  # noqa: BLE001 - telemetry must never stop the agent
            logger.debug("MQTT publish failed: %s", error)

    def close(self) -> None:
        self._client.loop_stop()
        self._client.disconnect()


def create_publisher(host: str | None, port: int, topic: str, client_id: str):
    if not host:
        return NullPublisher()
    if mqtt is None:
        logger.info("paho-mqtt not installed, MQTT publishing disabled")
        return NullPublisher()
    try:
        return MqttPublisher(host, port, topic, client_id)
    except Exception as error:  # noqa: BLE001
        logger.warning("MQTT disabled, could not start client: %s", error)
        return NullPublisher()
