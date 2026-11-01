"""MineSense Safety Edge agent entry point (cabin gateway).

See README.md for every environment variable. Typical run against the local
environment (docker compose): ``EDGE_SYNC_SERVICE_URL=http://localhost:5280``.
"""

from __future__ import annotations

import logging
import os
import random
import signal
import sys

from minesense_edge.agent import EdgeAgent
from minesense_edge.auth import create_token_provider
from minesense_edge.buffer import OfflineBuffer
from minesense_edge.cabin_warnings import WarningDispatcher, create_channels
from minesense_edge.cloud_client import SyncClient
from minesense_edge.config import EdgeConfig
from minesense_edge.connectivity import ConnectivityMonitor
from minesense_edge.mqtt_publisher import create_publisher
from minesense_edge.signals import SignalGenerator


def _handle_sigterm(signum, frame):  # noqa: ARG001
    raise KeyboardInterrupt


def build_agent(config: EdgeConfig) -> tuple[EdgeAgent, OfflineBuffer, object]:
    buffer = OfflineBuffer(config.buffer_path, max_events=config.max_buffered_events)
    # Ids survive restarts so the service sees one edge node with a growing SequenceNumber.
    edge_node_id = buffer.get_or_create_id("edge_node_id", config.edge_node_id)
    device_id = buffer.get_or_create_id("device_id", config.device_id)
    operator_id = buffer.get_or_create_id("operator_id", config.operator_id)
    session_id = buffer.get_or_create_id("monitoring_session_id", config.monitoring_session_id)

    tokens = create_token_provider(config.identity_url, config.access_token, config.username,
                                   config.password, timeout_seconds=config.http_timeout_seconds)
    client = SyncClient(config.sync_service_url, edge_node_id, device_id, tokens,
                        timeout_seconds=config.http_timeout_seconds)
    publisher = create_publisher(config.mqtt_host, config.mqtt_port,
                                 config.mqtt_topic or f"minesense/cabins/{device_id}/signals",
                                 client_id=f"edge-{edge_node_id}")
    agent = EdgeAgent(
        operator_id=operator_id,
        monitoring_session_id=session_id,
        generator=SignalGenerator(config.profile, random.Random(config.random_seed), config.cycle_length),
        buffer=buffer,
        client=client,
        dispatcher=WarningDispatcher(create_channels(config.unavailable_warning_channels)),
        monitor=ConnectivityMonitor(config.retry_initial_seconds, config.retry_max_seconds),
        publisher=publisher,
        batch_size=config.batch_size,
        max_batches_per_tick=config.max_batches_per_tick,
    )
    logging.getLogger("minesense_edge").info(
        "Edge agent started: edgeNode=%s device=%s operator=%s profile=%s sync=%s pending=%d",
        edge_node_id, device_id, operator_id, config.profile, config.sync_service_url, len(buffer))
    return agent, buffer, publisher


def main() -> int:
    logging.basicConfig(
        level=os.environ.get("LOG_LEVEL", "INFO").upper(),
        format="%(asctime)s %(levelname)s %(name)s: %(message)s",
    )
    signal.signal(signal.SIGTERM, _handle_sigterm)
    config = EdgeConfig.from_env()
    agent, buffer, publisher = build_agent(config)
    logger = logging.getLogger("minesense_edge")
    try:
        agent.run(config.interval_seconds, config.max_iterations)
        agent.synchronize(force=True)
    except KeyboardInterrupt:
        logger.info("Stopping Edge agent")
    finally:
        logger.info("%d event(s) pending in %s", len(buffer), buffer.path)
        publisher.close()
        buffer.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
