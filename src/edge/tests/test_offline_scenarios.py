"""US13 / US14 acceptance scenarios against an in-process fake cloud."""

import socket
import time

from minesense_edge.agent import READING_EVENT, WARNING_EVENT
from minesense_edge.auth import PasswordTokenProvider
from minesense_edge.buffer import OfflineBuffer
from minesense_edge.classifier import RiskLevel
from minesense_edge.connectivity import ConnectionState


def closed_port_url():
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        port = probe.getsockname()[1]
    return f"http://127.0.0.1:{port}"


def pending_types(buffer):
    return [event.event_type for event in buffer.pending_events()]


# ---------- US13: Protección durante interrupciones de comunicación ----------
def test_us13_detection_and_warnings_continue_when_communication_is_lost(cloud, make_agent):
    harness = make_agent(cloud.url, profile="microsleep")
    harness.agent.tick()
    assert harness.agent.monitor.state is ConnectionState.ONLINE

    cloud.mode = "down"
    risks = [harness.agent.tick() for _ in range(3)]

    assert risks == [RiskLevel.CRITICAL] * 3
    assert harness.agent.monitor.state is ConnectionState.OFFLINE


def test_us13_fatigue_without_communication_warns_and_keeps_event_pending(make_agent, caplog):
    harness = make_agent(closed_port_url(), profile="drowsy")

    with caplog.at_level("WARNING", logger="minesense_edge.cabin"):
        assert harness.agent.tick() is RiskLevel.WARNING

    assert "[PANTALLA] WARNING" in caplog.text
    assert len(harness.buffer) == 2
    assert pending_types(harness.buffer) == [WARNING_EVENT, READING_EVENT]


def test_us13_unavailable_functions_are_reported_without_stopping_protection(make_agent):
    harness = make_agent(closed_port_url(), profile="microsleep")

    for _ in range(3):
        assert harness.agent.tick() is RiskLevel.CRITICAL

    lost_notices = [n for n in harness.notices if n.startswith("Sin comunicación")]
    assert len(lost_notices) == 1  # reported once per interruption, not every tick
    assert "notificación al supervisor" in lost_notices[0]
    assert "advertencias en cabina siguen activas" in lost_notices[0]


def test_us13_unavailable_warning_channel_does_not_stop_the_warning(make_agent):
    harness = make_agent(closed_port_url(), profile="microsleep")
    harness.buzzer.available = False

    harness.agent.tick()

    warning = harness.buffer.pending_events()[0]
    assert warning.event_type == WARNING_EVENT
    assert warning.payload["deliveredChannels"] == ["display"]
    assert warning.payload["unavailableChannels"] == ["buzzer"]
    assert warning.payload["issuedOffline"] is False  # first failure happens after the warning
    assert any("no disponible: buzzer" in n for n in harness.notices)


# ---------- US14: Recuperación del historial pendiente ----------
def test_us14_pending_events_are_synced_in_order_after_reconnection(cloud, make_agent):
    harness = make_agent(cloud.url, profile="cycle", batch_size=3)
    cloud.mode = "down"
    for _ in range(7):
        harness.agent.tick()
    offline_ids = [e.event_id for e in harness.buffer.pending_events()]
    assert cloud.batches == {}

    cloud.mode = "online"
    harness.agent.tick()

    assert harness.agent.monitor.state is ConnectionState.ONLINE
    assert any(n.startswith("Comunicación con el centro de control restablecida") for n in harness.notices)
    stored = cloud.stored_event_ids()
    assert stored[:len(offline_ids)] == offline_ids
    sequences = [cloud.batches[b]["sequenceNumber"] for b in cloud.order]
    assert sequences == sorted(sequences)
    assert len(harness.buffer) == 0


def test_us14_interrupted_recovery_continues_without_losing_events(cloud, make_agent):
    harness = make_agent(cloud.url, profile="normal", batch_size=2, max_batches=10)
    cloud.mode = "down"
    for _ in range(6):
        harness.agent.tick()
    expected = [e.event_id for e in harness.buffer.pending_events()]

    cloud.mode, cloud.go_down_after = "online", 1  # link drops again after one batch
    harness.agent.synchronize()
    assert len(cloud.batches) == 1
    assert harness.agent.monitor.state is ConnectionState.OFFLINE

    cloud.mode, cloud.go_down_after = "online", None
    harness.agent.synchronize()

    assert cloud.stored_event_ids() == expected
    assert len(harness.buffer) == 0


def test_us14_event_already_in_history_is_not_registered_again(cloud, make_agent):
    harness = make_agent(cloud.url, profile="microsleep")
    cloud.mode = "down"
    harness.agent.tick()

    cloud.mode = "drop_after_store"   # stored by the service, answer lost
    harness.agent.synchronize()
    assert len(cloud.batches) == 1 and len(harness.buffer) == 2

    cloud.mode = "online"
    harness.agent.synchronize()

    batch_ids = [request["body"]["batchId"] for request in cloud.requests]
    assert len(set(batch_ids[-2:])) == 1          # same BatchId resent
    assert len(cloud.stored_event_ids()) == len(set(cloud.stored_event_ids())) == 2
    assert len(harness.buffer) == 0


def test_pending_events_survive_a_gateway_restart(cloud, make_agent, tmp_path):
    harness = make_agent(cloud.url, profile="microsleep")
    cloud.mode = "down"
    harness.agent.tick()
    harness.buffer.close()

    reopened = OfflineBuffer(tmp_path / "edge-buffer.db")
    assert len(reopened) == 2
    reopened.close()


def test_sync_uses_identity_access_tokens(cloud, make_agent):
    cloud.require_token = True
    tokens = PasswordTokenProvider(cloud.url, cloud.username, cloud.password)
    harness = make_agent(cloud.url, profile="normal", token_provider=tokens)

    harness.agent.tick()

    assert cloud.sign_ins == 1
    assert cloud.requests[-1]["headers"]["Authorization"] == "Bearer token-1"
    assert len(harness.buffer) == 0


def test_wrong_edge_credentials_keep_events_instead_of_discarding_them(cloud, make_agent):
    cloud.require_token = True
    tokens = PasswordTokenProvider(cloud.url, cloud.username, "wrong")
    harness = make_agent(cloud.url, profile="normal", token_provider=tokens)

    harness.agent.tick()

    assert harness.agent.monitor.state is ConnectionState.OFFLINE
    assert len(harness.buffer) == 1
    assert harness.buffer.dead_letter_count() == 0


def test_run_keeps_detecting_while_the_background_worker_syncs(cloud, make_agent):
    harness = make_agent(cloud.url, profile="microsleep")

    harness.agent.run(interval_seconds=0.05, max_iterations=5, sleep=time.sleep)
    harness.agent.synchronize(force=True)

    assert len(cloud.stored_event_ids()) == 10  # 5 readings + 5 critical warnings
    assert len(harness.buffer) == 0


def test_run_without_network_is_not_delayed_by_timeouts(make_agent):
    harness = make_agent(closed_port_url(), profile="drowsy")

    started = time.monotonic()
    harness.agent.run(interval_seconds=0, max_iterations=20, sleep=lambda _: None)

    assert len(harness.buffer) == 40
    assert time.monotonic() - started < 5
