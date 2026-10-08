from unittest.mock import MagicMock

import pytest
import requests

from minesense_edge.cloud_client import CloudClient
from minesense_edge.errors import CloudRejectedError, CloudUnavailableError

OPERATOR_ID = "11111111-1111-1111-1111-111111111111"
SESSION_ID = "22222222-2222-2222-2222-222222222222"
ASSESSMENT_ID = "33333333-3333-3333-3333-333333333333"


def response(status, body=None):
    mock = MagicMock()
    mock.status_code = status
    mock.json.return_value = body or {}
    mock.text = ""
    return mock


def make_event(local_risk="Normal"):
    return {
        "id": "event-1",
        "localRiskLevel": local_risk,
        "assessment": {
            "operatorId": OPERATOR_ID,
            "monitoringSessionId": SESSION_ID,
            "perclos": 0.5,
            "blinkRatePerMinute": 10.0,
            "heartRateVariabilityMs": 15.0,
        },
    }


def make_client(*responses):
    session = MagicMock(spec=requests.Session)
    session.post.side_effect = list(responses)
    client = CloudClient("http://fatigue:8080/", "http://alerts:8080", timeout_seconds=2, session=session)
    return client, session


def test_posts_assessment_resource_to_fatigue_service():
    client, session = make_client(response(201, {"id": ASSESSMENT_ID, "riskLevel": "Normal"}))
    event = make_event()

    client.deliver(event)

    session.post.assert_called_once_with(
        "http://fatigue:8080/api/v1/fatigue-assessments",
        json={
            "operatorId": OPERATOR_ID,
            "monitoringSessionId": SESSION_ID,
            "perclos": 0.5,
            "blinkRatePerMinute": 10.0,
            "heartRateVariabilityMs": 15.0,
        },
        timeout=2,
    )
    assert event["assessmentId"] == ASSESSMENT_ID


def test_critical_assessment_issues_alert():
    client, session = make_client(
        response(201, {"id": ASSESSMENT_ID, "riskLevel": "Critical"}),
        response(201, {"id": "alert-1"}),
    )
    event = make_event("Critical")

    client.deliver(event)

    alert_call = session.post.call_args_list[1]
    assert alert_call.args == ("http://alerts:8080/api/v1/alerts",)
    assert alert_call.kwargs["json"] == {
        "operatorId": OPERATOR_ID,
        "assessmentId": ASSESSMENT_ID,
        "severity": "Critical",
    }
    assert event["alertIssued"] is True


@pytest.mark.parametrize("cloud_risk", ["Normal", "Warning"])
def test_non_critical_assessment_does_not_issue_alert(cloud_risk):
    client, session = make_client(response(201, {"id": ASSESSMENT_ID, "riskLevel": cloud_risk}))
    client.deliver(make_event(cloud_risk))
    assert session.post.call_count == 1


def test_retry_after_alert_failure_does_not_duplicate_assessment():
    client, session = make_client(
        response(201, {"id": ASSESSMENT_ID, "riskLevel": "Critical"}),
        response(503),
        response(201, {"id": "alert-1"}),
    )
    event = make_event("Critical")

    with pytest.raises(CloudUnavailableError):
        client.deliver(event)
    client.deliver(event)

    urls = [call.args[0] for call in session.post.call_args_list]
    assert urls == [
        "http://fatigue:8080/api/v1/fatigue-assessments",
        "http://alerts:8080/api/v1/alerts",
        "http://alerts:8080/api/v1/alerts",
    ]
    assert event["alertIssued"] is True


def test_connection_error_is_unavailable():
    session = MagicMock(spec=requests.Session)
    session.post.side_effect = requests.ConnectionError("refused")
    client = CloudClient("http://fatigue:8080", "http://alerts:8080", session=session)
    with pytest.raises(CloudUnavailableError):
        client.deliver(make_event())


def test_client_error_is_rejected():
    client, _ = make_client(response(422))
    with pytest.raises(CloudRejectedError):
        client.deliver(make_event())
