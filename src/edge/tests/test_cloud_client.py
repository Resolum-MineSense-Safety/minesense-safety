from unittest.mock import MagicMock

import pytest
import requests

from conftest import DEVICE_ID, EDGE_NODE_ID
from minesense_edge.auth import PasswordTokenProvider, StaticTokenProvider
from minesense_edge.cloud_client import SyncClient
from minesense_edge.errors import CloudAuthError, CloudRejectedError, CloudUnavailableError


def sealed(buffer, count=2):
    for n in range(count):
        buffer.append("FatigueReadingClassified", {"n": n}, occurred_at=f"2026-11-01T10:00:0{n}+00:00")
    return buffer.next_batch(100)


def response(status, body=None):
    mock = MagicMock()
    mock.status_code = status
    mock.json.return_value = body or {}
    mock.text = ""
    return mock


def test_resource_matches_submit_synchronization_batch_resource(buffer):
    batch = sealed(buffer)
    resource = SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID).to_resource(batch)

    assert set(resource) == {"batchId", "edgeNodeId", "deviceId", "sequenceNumber", "capturedAt", "events"}
    assert resource["batchId"] == batch.batch_id
    assert resource["edgeNodeId"] == EDGE_NODE_ID and resource["deviceId"] == DEVICE_ID
    assert resource["sequenceNumber"] == 0
    assert resource["events"][0] == {"eventId": batch.events[0].event_id,
                                     "occurredAt": "2026-11-01T10:00:00+00:00",
                                     "eventType": "FatigueReadingClassified", "payload": {"n": 0}}


def test_new_batch_is_accepted_and_resend_is_reported_as_duplicate(cloud, buffer):
    batch = sealed(buffer)
    client = SyncClient(cloud.url, EDGE_NODE_ID, DEVICE_ID)

    assert client.submit(batch).duplicate is False
    assert client.submit(batch).duplicate is True
    assert len(cloud.batches) == 1


def test_service_down_is_unavailable(cloud, buffer):
    cloud.mode = "down"
    with pytest.raises(CloudUnavailableError):
        SyncClient(cloud.url, EDGE_NODE_ID, DEVICE_ID).submit(sealed(buffer))


def test_unreachable_host_is_unavailable(buffer):
    session = MagicMock()
    session.post.side_effect = requests.ConnectionError("refused")
    with pytest.raises(CloudUnavailableError):
        SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, session=session).submit(sealed(buffer))


@pytest.mark.parametrize("status", [400, 422])
def test_invalid_batch_is_rejected(status, buffer):
    session = MagicMock()
    session.post.return_value = response(status)
    with pytest.raises(CloudRejectedError):
        SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, session=session).submit(sealed(buffer))


@pytest.mark.parametrize("status", [408, 429, 500, 503])
def test_transient_status_is_unavailable(status, buffer):
    session = MagicMock()
    session.post.return_value = response(status)
    with pytest.raises(CloudUnavailableError):
        SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, session=session).submit(sealed(buffer))


def test_bearer_token_is_sent(buffer):
    session = MagicMock()
    session.post.return_value = response(202, {"status": "Accepted"})
    client = SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, StaticTokenProvider("abc"), session=session)

    client.submit(sealed(buffer))

    assert session.post.call_args.kwargs["headers"] == {"Authorization": "Bearer abc"}


def test_expired_token_is_renewed_once_and_batch_is_sent(cloud, buffer):
    cloud.require_token = True
    tokens = PasswordTokenProvider(cloud.url, cloud.username, cloud.password)
    client = SyncClient(cloud.url, EDGE_NODE_ID, DEVICE_ID, tokens)
    client.submit(sealed(buffer, 1))
    cloud.revoke_tokens()

    buffer.mark_delivered(buffer.next_batch(10).batch_id)
    client.submit(sealed(buffer, 1))

    assert cloud.sign_ins == 2
    assert len(cloud.batches) == 2


def test_refused_token_is_an_auth_error_that_keeps_events(buffer):
    session = MagicMock()
    session.post.return_value = response(401)
    client = SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, StaticTokenProvider("old"), session=session)

    with pytest.raises(CloudAuthError) as error:
        client.submit(sealed(buffer))
    assert isinstance(error.value, CloudUnavailableError)
    assert session.post.call_count == 2


def test_forbidden_is_an_auth_error(buffer):
    session = MagicMock()
    session.post.return_value = response(403)
    with pytest.raises(CloudAuthError):
        SyncClient("http://sync", EDGE_NODE_ID, DEVICE_ID, session=session).submit(sealed(buffer))
