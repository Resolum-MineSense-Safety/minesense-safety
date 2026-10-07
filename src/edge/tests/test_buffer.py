import json

import pytest

from minesense_edge.buffer import OfflineBuffer
from minesense_edge.errors import CloudRejectedError, CloudUnavailableError


def events(n):
    return [{"id": f"e{i}"} for i in range(n)]


def test_append_persists_to_json_file(tmp_path):
    path = tmp_path / "buffer.json"
    buffer = OfflineBuffer(path)
    for event in events(3):
        buffer.append(event)

    assert json.loads(path.read_text(encoding="utf-8")) == events(3)


def test_survives_restart(tmp_path):
    path = tmp_path / "nested" / "buffer.json"
    first = OfflineBuffer(path)
    for event in events(3):
        first.append(event)

    restarted = OfflineBuffer(path)

    assert len(restarted) == 3
    assert restarted.snapshot() == events(3)


def test_flush_sends_in_order_and_empties(tmp_path):
    buffer = OfflineBuffer(tmp_path / "buffer.json")
    for event in events(4):
        buffer.append(event)
    sent = []

    removed = buffer.flush(lambda event: sent.append(event["id"]))

    assert removed == 4
    assert sent == ["e0", "e1", "e2", "e3"]
    assert len(buffer) == 0
    assert OfflineBuffer(tmp_path / "buffer.json").snapshot() == []


def test_keeps_events_while_offline_and_flushes_in_order_later(tmp_path):
    path = tmp_path / "buffer.json"
    buffer = OfflineBuffer(path)
    online = {"value": False}
    sent = []

    def send(event):
        if not online["value"]:
            raise CloudUnavailableError("no route to cloud")
        sent.append(event["id"])

    for event in events(3):
        buffer.append(event)
        buffer.flush(send)
    assert len(buffer) == 3 and sent == []

    # Gateway reboots while still offline, then connectivity returns
    restarted = OfflineBuffer(path)
    online["value"] = True
    restarted.append({"id": "e3"})
    restarted.flush(send)

    assert sent == ["e0", "e1", "e2", "e3"]
    assert len(restarted) == 0


def test_stops_at_first_failure_and_keeps_rest(tmp_path):
    buffer = OfflineBuffer(tmp_path / "buffer.json")
    for event in events(3):
        buffer.append(event)
    sent = []

    def send(event):
        if event["id"] == "e1":
            raise CloudUnavailableError("timeout")
        sent.append(event["id"])

    assert buffer.flush(send) == 1
    assert sent == ["e0"]
    assert [e["id"] for e in buffer.snapshot()] == ["e1", "e2"]


def test_partial_progress_on_head_event_is_persisted(tmp_path):
    path = tmp_path / "buffer.json"
    buffer = OfflineBuffer(path)
    buffer.append({"id": "e0"})

    def send(event):
        event["assessmentId"] = "a-1"
        raise CloudUnavailableError("alert service down")

    buffer.flush(send)

    assert OfflineBuffer(path).snapshot() == [{"id": "e0", "assessmentId": "a-1"}]


def test_rejected_event_is_discarded_and_flush_continues(tmp_path):
    buffer = OfflineBuffer(tmp_path / "buffer.json")
    for event in events(3):
        buffer.append(event)
    sent = []

    def send(event):
        if event["id"] == "e1":
            raise CloudRejectedError("422")
        sent.append(event["id"])

    assert buffer.flush(send) == 3
    assert sent == ["e0", "e2"]


def test_drops_oldest_when_full(tmp_path):
    buffer = OfflineBuffer(tmp_path / "buffer.json", max_events=2)
    for event in events(3):
        buffer.append(event)
    assert [e["id"] for e in buffer.snapshot()] == ["e1", "e2"]


def test_corrupt_file_starts_empty(tmp_path):
    path = tmp_path / "buffer.json"
    path.write_text("{not json", encoding="utf-8")
    assert len(OfflineBuffer(path)) == 0


def test_invalid_max_events():
    with pytest.raises(ValueError):
        OfflineBuffer("unused.json", max_events=0)
