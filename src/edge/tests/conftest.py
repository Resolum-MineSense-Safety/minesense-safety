import random

import pytest

from fake_cloud import FakeCloud
from minesense_edge.agent import EdgeAgent
from minesense_edge.buffer import OfflineBuffer
from minesense_edge.cabin_warnings import BuzzerChannel, CabinDisplayChannel, WarningDispatcher
from minesense_edge.cloud_client import SyncClient
from minesense_edge.connectivity import ConnectivityMonitor
from minesense_edge.signals import SignalGenerator

EDGE_NODE_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa"
DEVICE_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb"


@pytest.fixture
def cloud():
    server = FakeCloud().start()
    yield server
    server.stop()


@pytest.fixture
def buffer(tmp_path):
    store = OfflineBuffer(tmp_path / "edge-buffer.db")
    yield store
    store.close()


class Harness:
    def __init__(self, agent, buffer, display, buzzer, notices):
        self.agent = agent
        self.buffer = buffer
        self.display = display
        self.buzzer = buzzer
        self.notices = notices


@pytest.fixture
def make_agent(buffer):
    def factory(url, profile="microsleep", token_provider=None, batch_size=100, max_batches=5):
        display, buzzer = CabinDisplayChannel(), BuzzerChannel()
        dispatcher = WarningDispatcher([display, buzzer])
        notices = []
        original_notify = dispatcher.notify

        def recording_notify(message):
            notices.append(message)
            return original_notify(message)

        dispatcher.notify = recording_notify
        agent = EdgeAgent(
            operator_id="11111111-1111-4111-8111-111111111111",
            monitoring_session_id="22222222-2222-4222-8222-222222222222",
            generator=SignalGenerator(profile, random.Random(7)),
            buffer=buffer,
            client=SyncClient(url, EDGE_NODE_ID, DEVICE_ID, token_provider, timeout_seconds=2),
            dispatcher=dispatcher,
            # No backoff in tests: every tick may try to synchronize.
            monitor=ConnectivityMonitor(0, 0),
            batch_size=batch_size,
            max_batches_per_tick=max_batches,
        )
        return Harness(agent, buffer, display, buzzer, notices)

    return factory
