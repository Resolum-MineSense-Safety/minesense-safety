"""HTTP client for the Fatigue Detection and Alerts microservices."""

from __future__ import annotations

import logging

import requests

from .classifier import RiskLevel
from .errors import CloudRejectedError, CloudUnavailableError

logger = logging.getLogger(__name__)

FATIGUE_ASSESSMENTS_PATH = "/api/v1/fatigue-assessments"
ALERTS_PATH = "/api/v1/alerts"


class CloudClient:
    def __init__(
        self,
        fatigue_service_url: str,
        alert_service_url: str,
        timeout_seconds: float = 3.0,
        session: requests.Session | None = None,
    ) -> None:
        self._assessments_url = fatigue_service_url.rstrip("/") + FATIGUE_ASSESSMENTS_PATH
        self._alerts_url = alert_service_url.rstrip("/") + ALERTS_PATH
        self._timeout = timeout_seconds
        self._session = session or requests.Session()

    def post_assessment(self, payload: dict) -> dict:
        """POST an AssessFatigueResource; returns the FatigueAssessmentResource."""
        return self._post(self._assessments_url, payload)

    def issue_alert(self, operator_id: str, assessment_id: str, severity: str) -> dict:
        """POST an IssueAlertResource; returns the AlertResource."""
        return self._post(
            self._alerts_url,
            {"operatorId": operator_id, "assessmentId": assessment_id, "severity": severity},
        )

    def deliver(self, event: dict) -> None:
        """Synchronize one buffered event.

        Progress is recorded on the event (``assessmentId``, ``alertIssued``) so a
        retry after a partial failure never duplicates the assessment.
        """
        payload = event["assessment"]
        if not event.get("assessmentId"):
            assessment = self.post_assessment(payload)
            event["assessmentId"] = assessment["id"]
            event["cloudRiskLevel"] = assessment.get("riskLevel")

        risk = event.get("cloudRiskLevel") or event.get("localRiskLevel")
        if risk == RiskLevel.CRITICAL.value and not event.get("alertIssued"):
            alert = self.issue_alert(
                payload["operatorId"], event["assessmentId"], RiskLevel.CRITICAL.value
            )
            event["alertIssued"] = True
            logger.warning("Critical alert %s issued for operator %s", alert.get("id"), payload["operatorId"])

    def _post(self, url: str, body: dict) -> dict:
        try:
            response = self._session.post(url, json=body, timeout=self._timeout)
        except requests.RequestException as error:
            raise CloudUnavailableError(f"POST {url} failed: {error}") from error

        if response.status_code >= 500:
            raise CloudUnavailableError(f"POST {url} returned {response.status_code}")
        if response.status_code >= 400:
            raise CloudRejectedError(
                f"POST {url} returned {response.status_code}: {response.text[:200]}"
            )
        return response.json()
