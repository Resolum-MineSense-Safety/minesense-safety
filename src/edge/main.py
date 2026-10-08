"""MineSense Safety Edge agent entry point (cabin gateway simulator).

Configuration (environment variables):
  FATIGUE_SERVICE_URL        default http://localhost:5158
  ALERT_SERVICE_URL          default http://localhost:5233
  EDGE_OPERATOR_ID           operator GUID (random if empty)
  EDGE_MONITORING_SESSION_ID monitoring session GUID (random if empty)
  EDGE_PROFILE               normal | drowsy | microsleep | cycle (default cycle)
  EDGE_CYCLE_LENGTH          readings per profile when EDGE_PROFILE=cycle (default 10)
  EDGE_INTERVAL_SECONDS      seconds between readings (default 2)
  EDGE_MAX_ITERATIONS        stop after N readings, 0 = run forever (default 0)
  EDGE_BUFFER_PATH           offline buffer file (default data/offline-buffer.json)
  EDGE_HTTP_TIMEOUT_SECONDS  HTTP timeout (default 3)
  EDGE_RANDOM_SEED           seed for reproducible signals (optional)
  MQTT_HOST / MQTT_PORT / MQTT_TOPIC  optional MQTT telemetry
  LOG_LEVEL                  default INFO
"""

from __future__ import annotations

import logging
import os
import random
import signal
import sys

from minesense_edge.agent import EdgeAgent
from minesense_edge.buffer import OfflineBuffer
from minesense_edge.cloud_client import CloudClient
from minesense_edge.config import EdgeConfig
from minesense_edge.mqtt_publisher import create_publisher
from minesense_edge.signals import SignalGenerator


def _handle_sigterm(signum, frame):  # noqa: ARG001
    raise KeyboardInterrupt


def main() -> int:
    logging.basicConfig(
        level=os.environ.get("LOG_LEVEL", "INFO").upper(),
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )
    signal.signal(signal.SIGTERM, _handle_sigterm)
    config = EdgeConfig.from_env()
    logger = logging.getLogger("minesense_edge")
    logger.info(
        "MineSense Safety Edge agent started: operator=%s profile=%s fatigue=%s alerts=%s",
        config.operator_id, config.profile, config.fatigue_service_url, config.alert_service_url,
    )

    buffer = OfflineBuffer(config.buffer_path)
    if len(buffer):
        logger.info("Recovered %d pending event(s) from %s", len(buffer), buffer.path)

    client = CloudClient(config.fatigue_service_url, config.alert_service_url,
                         timeout_seconds=config.http_timeout_seconds)
    publisher = create_publisher(config.mqtt_host, config.mqtt_port, config.mqtt_topic,
                                 client_id=f"edge-{config.operator_id}")
    agent = EdgeAgent(
        operator_id=config.operator_id,
        monitoring_session_id=config.monitoring_session_id,
        generator=SignalGenerator(config.profile, random.Random(config.random_seed),
                                  config.cycle_length),
        buffer=buffer,
        deliver=client.deliver,
        publisher=publisher,
    )

    try:
        agent.run(config.interval_seconds, config.max_iterations)
    except KeyboardInterrupt:
        logger.info("Stopping Edge agent, %d event(s) pending in buffer", len(buffer))
    finally:
        publisher.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
