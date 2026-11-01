import sqlite3

import pytest

from minesense_edge.buffer import OfflineBuffer


def test_events_survive_a_restart_of_the_gateway(tmp_path):
    path = tmp_path / "edge.db"
    first = OfflineBuffer(path)
    first.append("FatigueReadingClassified", {"n": 1})
    first.append("FatigueReadingClassified", {"n": 2})
    first.close()

    reopened = OfflineBuffer(path)
    assert len(reopened) == 2
    batch = reopened.next_batch(10)
    assert [event.payload["n"] for event in batch.events] == [1, 2]
    reopened.close()


def test_a_sealed_batch_keeps_its_id_until_it_is_delivered(buffer):
    for n in range(3):
        buffer.append("FatigueReadingClassified", {"n": n})

    first = buffer.next_batch(2)
    buffer.append("FatigueReadingClassified", {"n": 3})
    retry = buffer.next_batch(2)

    assert retry.batch_id == first.batch_id
    assert retry.events == first.events
    assert [e.payload["n"] for e in first.events] == [0, 1]


def test_batches_are_numbered_in_order_and_keep_capture_order(buffer):
    for n in range(5):
        buffer.append("FatigueReadingClassified", {"n": n})

    seen = []
    while (batch := buffer.next_batch(2)) is not None:
        seen.append((batch.sequence_number, [e.payload["n"] for e in batch.events]))
        assert buffer.mark_delivered(batch.batch_id) == len(batch.events)

    assert seen == [(0, [0, 1]), (1, [2, 3]), (2, [4])]
    assert len(buffer) == 0


def test_sequence_number_continues_after_restart(tmp_path):
    path = tmp_path / "edge.db"
    store = OfflineBuffer(path)
    store.append("E", {})
    store.mark_delivered(store.next_batch(10).batch_id)
    store.close()

    store = OfflineBuffer(path)
    store.append("E", {})
    assert store.next_batch(10).sequence_number == 1
    store.close()


def test_rejected_batch_goes_to_dead_letters_instead_of_being_lost(buffer):
    buffer.append("E", {"n": 1})
    batch = buffer.next_batch(10)

    assert buffer.mark_rejected(batch.batch_id, "400 invalid") == 1
    assert len(buffer) == 0
    assert buffer.dead_letter_count() == 1
    assert buffer.next_batch(10) is None


def test_full_buffer_drops_oldest_normal_reading_before_any_warning(tmp_path):
    store = OfflineBuffer(tmp_path / "edge.db", max_events=3)
    store.append("CabinWarningIssued", {"n": "warning"}, priority=2)
    store.append("FatigueReadingClassified", {"n": "old-normal"}, priority=0)
    store.append("FatigueReadingClassified", {"n": "normal"}, priority=0)
    store.append("FatigueReadingClassified", {"n": "new"}, priority=0)

    payloads = [e.payload["n"] for e in store.next_batch(10).events]
    assert payloads == ["warning", "normal", "new"]
    store.close()


def test_ids_of_the_gateway_are_created_once_and_can_be_overridden(buffer):
    generated = buffer.get_or_create_id("edge_node_id")
    assert buffer.get_or_create_id("edge_node_id") == generated
    assert buffer.get_or_create_id("edge_node_id", "fixed") == "fixed"
    assert buffer.get_or_create_id("edge_node_id") == "fixed"


def test_failed_write_rolls_back(buffer):
    buffer.append("E", {}, event_id="dup")
    with pytest.raises(sqlite3.IntegrityError):
        buffer.append("E", {}, event_id="dup")
    assert len(buffer) == 1


def test_max_events_must_be_positive(tmp_path):
    with pytest.raises(ValueError):
        OfflineBuffer(tmp_path / "x.db", max_events=0)


def test_pending_events_lists_sealed_batches_first_without_sealing(buffer):
    buffer.append("E", {"n": 0})
    buffer.append("E", {"n": 1})
    sealed = buffer.next_batch(1)
    buffer.append("E", {"n": 2})

    assert [e.payload["n"] for e in buffer.pending_events()] == [0, 1, 2]
    assert buffer.next_batch(10).batch_id == sealed.batch_id
