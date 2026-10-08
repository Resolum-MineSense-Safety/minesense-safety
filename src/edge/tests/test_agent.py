import random

from minesense_edge.agent import EdgeAgent
from minesense_edge.buffer import OfflineBuffer
from minesense_edge.classifier import RiskLevel
from minesense_edge.config import EdgeConfig
from minesense_edge.errors import CloudUnavailableError
from minesense_edge.signals import SignalGenerator


def make_agent(tmp_path, profile, deliver):
    alarms = []
    agent = EdgeAgent(
        operator_id="op-1",
        monitoring_session_id="session-1",
        generator=SignalGenerator(profile, random.Random(5)),
        buffer=OfflineBuffer(tmp_path / "buffer.json"),
        deliver=deliver,
        alarm=lambda reading, risk: alarms.append(risk),
    )
    return agent, alarms


def test_critical_reading_raises_cabin_alarm_while_offline(tmp_path):
    def offline(event):
        raise CloudUnavailableError("offline")

    agent, alarms = make_agent(tmp_path, "microsleep", offline)

    assert agent.tick() is RiskLevel.CRITICAL
    assert alarms == [RiskLevel.CRITICAL]
    assert len(OfflineBuffer(tmp_path / "buffer.json")) == 1


def test_events_are_delivered_when_online(tmp_path):
    delivered = []
    agent, alarms = make_agent(tmp_path, "normal", delivered.append)

    agent.run(interval_seconds=0, max_iterations=3, sleep=lambda _: None)

    assert len(delivered) == 3
    assert alarms == []
    assert delivered[0]["assessment"]["operatorId"] == "op-1"
    assert delivered[0]["localRiskLevel"] == "Normal"


def test_config_from_env_defaults_and_overrides():
    config = EdgeConfig.from_env({
        "FATIGUE_SERVICE_URL": "http://fatigue-detection-service:8080",
        "EDGE_OPERATOR_ID": "op-9",
        "EDGE_PROFILE": "drowsy",
        "EDGE_INTERVAL_SECONDS": "0.5",
    })
    assert config.fatigue_service_url == "http://fatigue-detection-service:8080"
    assert config.alert_service_url == "http://localhost:5233"
    assert config.operator_id == "op-9"
    assert config.profile == "drowsy"
    assert config.interval_seconds == 0.5
    assert config.mqtt_host is None
    assert config.mqtt_topic == "minesense/cabins/op-9/signals"
